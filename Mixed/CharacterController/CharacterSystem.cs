using System;
using DefaultNamespace;
using package.stormium.def;
using package.stormiumteam.shared.ecs;
using Unity.NetCode;
using Stormium.Default;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Assertions;
using CapsuleCollider = Unity.Physics.CapsuleCollider;
using Collider = Unity.Physics.Collider;
using SphereCollider = Unity.Physics.SphereCollider;

namespace CharacterController
{
	[InternalBufferCapacity(16)]
	public struct CharacterPass : IBufferElementData
	{
		public PhysicsCharacter             Character;
		public PhysicsCollider              Collider;
		public BlobAssetReference<Collider> Probe;

		public float3       Direction;
		public float3       Position;
		public quaternion   Rotation;
		public float3       Velocity;
		public GroundResult Ground;

		public LocalToWorld ToWorld => new LocalToWorld {Value = new float4x4(Rotation, Position)};

		public unsafe MoveData ToMoveData()
		{
			MoveData moveData;
			moveData.Character = Character;
			moveData.Probe     = (SphereCollider*) Probe.GetUnsafePtr();
			moveData.Collider  = (CapsuleCollider*) Collider.ColliderPtr;
			moveData.Position  = Position;
			moveData.Rotation  = Rotation;
			moveData.Velocity  = Velocity;
			return moveData;
		}
	}

	public static class CharacterPassExtension
	{
		public static bool TryGetPass(this DynamicBuffer<CharacterPass> buffer, int index, out CharacterPass pass)
		{
			pass = default;
			if (buffer.Length <= index || buffer.Length == 0)
				return false;

			pass = buffer[index];
			return true;
		}

		public static CharacterPass GetLastPass(this DynamicBuffer<CharacterPass> buffer)
		{
			if (buffer.Length == 0)
				return default;
			return buffer[buffer.Length - 1];
		}

		public static CharacterPass GetFirstPass(this DynamicBuffer<CharacterPass> buffer)
		{
			if (buffer.Length == 0)
				return default;
			return buffer[0];
		}
	}

	// Stop moving character (assigned and removed by CharacterMovementInitSystem)
	// Automatically assigned if the character has no health left or 'StopMovable' component is present
	public struct IgnoreCharacterMovement : IComponentData
	{
	}

	[UpdateInGroup(typeof(OrderGroup.Simulation.ConfigureSpawnedEntities))]
	[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
	public class CharacterSetupIgnoring : ComponentSystem
	{
		private EntityQuery m_RunningCharacterQuery;
		private EntityQuery m_IgnoredCharacterQuery;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_RunningCharacterQuery = GetEntityQuery(new EntityQueryDesc
			{
				All  = new ComponentType[] {typeof(CharacterDescription), typeof(PhysicsCharacter)},
				None = new ComponentType[] {typeof(IgnoreCharacterMovement)}
			});
			m_IgnoredCharacterQuery = GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[] {typeof(CharacterDescription), typeof(PhysicsCharacter), typeof(IgnoreCharacterMovement)}
			});
		}

		protected override void OnUpdate()
		{
			Entities.With(m_RunningCharacterQuery).ForEach(ent =>
			{
				var add = false;
				if (EntityManager.HasComponent<LivableHealth>(ent))
				{
					var health = EntityManager.GetComponentData<LivableHealth>(ent);
					if (health.IsDead)
						add = true;
				}
				// todo: check for StopMovable

				if (add)
				{
					EntityManager.AddComponent(ent, typeof(IgnoreCharacterMovement));
				}
			});
			Entities.With(m_IgnoredCharacterQuery).ForEach(ent =>
			{
				var remove = false;
				if (EntityManager.HasComponent<LivableHealth>(ent))
				{
					var health = EntityManager.GetComponentData<LivableHealth>(ent);
					if (!health.IsDead)
						remove = true;
				}

				if (remove)
				{
					EntityManager.RemoveComponent(ent, typeof(IgnoreCharacterMovement));
				}
			});
		}
	}

	[UpdateInGroup(typeof(CharacterInteractionGroup))]
	[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
	public class CharacterMovementInitSystem : JobGameBaseSystem
	{
		private LazySystem<BuildPhysicsWorld> m_BuildPhysicWorld;
		private JobPhysicsQuery               m_Query;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_Query = new JobPhysicsQuery(() => SphereCollider.Create(new SphereGeometry {Radius = 0.1f}));
		}

		protected override unsafe JobHandle OnUpdate(JobHandle inputDeps)
		{
			var query     = m_Query;
			var physWorld = this.L(ref m_BuildPhysicWorld).PhysicsWorld;
			return Entities
			       .ForEach((ref DynamicBuffer<CharacterPass> passes, ref Rotation rot, in CharacterInput input, in PhysicsCharacter physChar, in PhysicsCollider charColl, in Translation pos, in Velocity vel) =>
			       {
				       if (charColl.ColliderPtr->Type != ColliderType.Capsule)
					       throw new InvalidOperationException("Only Capsule colliders are allowed for Character Movement.");

				       passes.Clear();

				       var capsuleColl = (CapsuleCollider*) charColl.ColliderPtr;
				       if (physChar.MaxStepHeight > capsuleColl->Radius)
					       throw new InvalidOperationException("1");

				       // update rotation
				       rot.Value = quaternion.AxisAngle(math.up(), math.radians(input.Look.x));

				       var     probeColl = query.Blob;
				       ref var coll      = ref probeColl.Value;
				       coll.Filter = charColl.ColliderPtr->Filter;

				       MoveData moveData;
				       moveData.Character = physChar;
				       moveData.Probe     = (SphereCollider*) probeColl.GetUnsafePtr();
				       moveData.Collider  = capsuleColl;
				       moveData.Position  = pos.Value;
				       moveData.Rotation  = rot.Value;
				       moveData.Velocity  = vel.Value;

				       var groundResult = PhysicsCharacter.CheckGround(moveData, physWorld);
				       var direction    = SrtMovement.ComputeDirection(rot.Value, input.Move);

				       CharacterPass initPass;
				       initPass.Character = physChar;
				       initPass.Collider  = charColl;
				       initPass.Probe     = probeColl;
				       initPass.Direction = direction;
				       initPass.Velocity  = vel.Value;
				       initPass.Position  = pos.Value;
				       initPass.Rotation  = rot.Value;
				       initPass.Ground    = groundResult;
				       passes.Add(initPass);
			       })
			       .WithReadOnly(physWorld)
			       .Schedule(JobHandle.CombineDependencies(inputDeps, m_BuildPhysicWorld.Value.FinalJobHandle));
		}
	}

	[UpdateInGroup(typeof(CharacterInteractionGroup))]
	//[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
	public class CharacterMovementEndSystem : JobGameBaseSystem
	{
		private LazySystem<BuildPhysicsWorld> m_BuildPhysicWorld;

		protected override unsafe JobHandle OnUpdate(JobHandle inputDeps)
		{
			var velocityFromEntity = GetComponentDataFromEntity<Velocity>(true);
			var physWorld          = this.L(ref m_BuildPhysicWorld).PhysicsWorld;
			var tick = World.GetExistingSystem<GhostPredictionSystemGroup>().PredictingTick;
			var delta = Time.DeltaTime;

			return Entities
			       .ForEach((ref DynamicBuffer<CharacterPass> passes, ref Translation pos, ref Velocity vel, in PhysicsCollider charColl, in GhostPredictedComponent predicted) =>
			       {
				       if (!GhostPredictionSystemGroup.ShouldPredict(tick, predicted))
					       return;
				       
				       if (charColl.ColliderPtr->Type != ColliderType.Capsule)
					       throw new InvalidOperationException("Only Capsule colliders are allowed for Character Movement.");

				       if (passes.Length == 0)
					       return;
				       
				       var gravity   = new float3(0, -15, 0);
				       var lastPass  = passes.GetLastPass();
				       var firstPass = passes.GetFirstPass();

				       var hadBecameUnsupported = firstPass.Velocity.y > 0.01f && (firstPass.Ground.State & GroundState.TouchGround) != 0;

				       var moveData   = lastPass.ToMoveData();
				       var moveEvents = new NativeList<MoveEvent>(16, Allocator.Temp);

				       // get supported velocity
				       var supportedVelocity = float3.zero;
				       if ((firstPass.Ground.State & GroundState.StableOnGround) != 0)
				       {
					       var supportedEntity = physWorld.Bodies[firstPass.Ground.RigidBodyIndex].Entity;
					       if (velocityFromEntity.Exists(supportedEntity))
					       {
						       supportedVelocity = velocityFromEntity[supportedEntity].Value;
					       }
				       }

				       if (hadBecameUnsupported || vel.Value.y > 0)
				       {
					       moveData.Velocity += supportedVelocity;
				       }

				       vel.Value = moveData.Velocity;
				       moveData.Velocity *= delta;

				       var moveResult = PhysicsCharacter.Move(moveData, physWorld, moveEvents);

				       var moveVector = math.normalizesafe(moveResult.NewPosition - moveData.Position);
				       if (vel.Value.y <= 0 && vel.speedSqr > 0 && moveVector.y <= 0 && (lastPass.Ground.State & GroundState.TouchGround) != 0 && (moveResult.GroundStatus.State & GroundState.TouchGround) == 0)
				       {
					       moveData.Position = moveResult.NewPosition;

					       if (PhysicsCharacter.StickOnGround(ref moveData, in physWorld, gravity * delta))
					       {
						       moveResult.NewPosition = moveData.Position;
					       }
				       }

				       for (var i = 0; i < moveEvents.Length; i++)
				       {
					       var ev = moveEvents[i];
					       if (ev.Type == MoveEventType.Obstacle)
					       {
						       vel.Value = RayUtility.SlideVelocityNoYChange(vel.Value, ev.SurfaceNormal);
					       }
				       }

				       pos.Value = moveResult.NewPosition;
				       if (moveResult.GroundStatus.State != GroundState.StableOnGround)
					       vel.Value += gravity * delta;
				       else
					       vel.Value.y = math.max(0.0f, vel.Value.y);
			       })
			       .WithReadOnly(velocityFromEntity)
			       .WithReadOnly(physWorld)
			       .WithNone<IgnoreCharacterMovement>()
			       .WithAll<PhysicsCharacter, Rotation>()
			       .Schedule(JobHandle.CombineDependencies(inputDeps, m_BuildPhysicWorld.Value.FinalJobHandle));
		}
	}
}