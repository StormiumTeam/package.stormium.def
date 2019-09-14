using System;
using Revolution.NetCode;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Scripts.Bumpers
{
	[Serializable]
	public struct LaunchPad : IComponentData
	{
		public float3 direction;
		public float3 momentum;
		public float  force;
	}

	[InternalBufferCapacity(16)]
	public struct LaunchPadCooldown : IBufferElementData, IEquatable<Entity>
	{
		public Entity Target;
		public UTick  RemoveAtTick;

		public bool Equals(Entity entity)
		{
			return Target == entity;
		}
	}

	[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
	public unsafe class LaunchPadSystem : JobGameBaseSystem
	{
		[BurstCompile]
		private struct Job : IJobForEachWithEntity<LocalToWorld, LaunchPad, PhysicsCollider>
		{
			[ReadOnly] public UTick Tick;

			[ReadOnly] public NativeArray<ArchetypeChunk>                  MovableChunks;
			[ReadOnly] public ArchetypeChunkEntityType                     EntityType;
			[ReadOnly] public ArchetypeChunkComponentType<LocalToWorld>    LocalToWorldType;
			[ReadOnly] public ArchetypeChunkComponentType<PhysicsCollider> PhysicsColliderType;

			[ReadOnly] public BufferFromEntity<LaunchPadCooldown> CooldownFromEntity;

			[WriteOnly] public NativeList<TargetImpulseEvent> ImpulseEventList;

			private bool Bump(Entity padEntity,     LocalToWorld padTransform,     LaunchPad       pad, PhysicsCollider padCollider,
			                  Entity movableEntity, LocalToWorld movableTransform, PhysicsCollider movableCollider)
			{
				var padRigidTransform     = new RigidTransform(padTransform.Value);
				var movableRigidTransform = new RigidTransform(movableTransform.Value);

				if (!padCollider.ColliderPtr->CalculateAabb(padRigidTransform).Overlaps(movableCollider.ColliderPtr->CalculateAabb(movableRigidTransform)))
					return false;

				var collection = new CustomCollideCollection(new CustomCollide(movableCollider, movableTransform));
				var penetrateInput = new ColliderDistanceInput
				{
					Collider    = padCollider.ColliderPtr,
					MaxDistance = 0f,
					Transform   = padRigidTransform
				};

				var anyCollector = new AnyHitCollector<DistanceHit>(0.0f);
				if (!collection.CalculateDistance(penetrateInput, ref anyCollector))
					return false;

				ImpulseEventList.Add(new TargetImpulseEvent
				{
					Origin      = padEntity,
					Destination = movableEntity,

					Force    = math.normalizesafe(pad.direction) * pad.force,
					Momentum = pad.momentum,
					Position = padTransform.Position,
				});

				return true;
			}

			public void Execute(Entity padEntity, int index, [ReadOnly] ref LocalToWorld padTransform, [ReadOnly] ref LaunchPad launchPad, [ReadOnly] ref PhysicsCollider padCollider)
			{
				var cooldownBuffer = CooldownFromEntity[padEntity];
				// Delete previous cooldown...
				for (var c = 0; c != cooldownBuffer.Length; c++)
				{
					if (cooldownBuffer[c].RemoveAtTick >= Tick)
						continue;

					cooldownBuffer.RemoveAt(c);
					c--;
				}

				// We can't add more cooldown elements...
				if (cooldownBuffer.Length >= cooldownBuffer.Capacity)
					return;

				for (var chk = 0; chk != MovableChunks.Length; chk++)
				{	
					var movableEntityArray    = MovableChunks[chk].GetNativeArray(EntityType);
					var movableTransformArray = MovableChunks[chk].GetNativeArray(LocalToWorldType);
					var movableColliderArray  = MovableChunks[chk].GetNativeArray(PhysicsColliderType);

					var count = MovableChunks[chk].Count;
					for (var ent = 0; ent != count; ent++)
					{
						if (cooldownBuffer.AsNativeArray().Contains(movableEntityArray[ent]))
							continue;

						if (Bump
						(
							padEntity, padTransform, launchPad, padCollider,
							movableEntityArray[ent], movableTransformArray[ent], movableColliderArray[ent]
						))
						{
							cooldownBuffer.Add(new LaunchPadCooldown {Target = movableEntityArray[ent], RemoveAtTick = UTick.AddMs(Tick, 100)});
						}
					}
				}
			}
		}

		private EntityQuery m_LaunchPadQuery;
		private EntityQuery m_MovableQuery;

		private TargetImpulseEvent.Provider m_TargetImpulseEventProvider;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_LaunchPadQuery = GetEntityQuery(typeof(LocalToWorld), typeof(LaunchPad), typeof(PhysicsCollider));
			m_MovableQuery   = GetEntityQuery(typeof(LocalToWorld), typeof(MovableDescription), typeof(PhysicsCollider));

			m_TargetImpulseEventProvider = World.GetOrCreateSystem<TargetImpulseEvent.Provider>();
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			m_MovableQuery.AddDependency(inputDeps);

			var movableChunks = m_MovableQuery.CreateArchetypeChunkArray(Allocator.TempJob, out var dependency);
			inputDeps = new Job
			{
				Tick = ServerSimulationSystemGroup.GetTick(),

				MovableChunks       = movableChunks,
				EntityType          = GetArchetypeChunkEntityType(),
				LocalToWorldType    = GetArchetypeChunkComponentType<LocalToWorld>(true),
				PhysicsColliderType = GetArchetypeChunkComponentType<PhysicsCollider>(true),

				ImpulseEventList = m_TargetImpulseEventProvider.GetEntityDelayedList(),

				CooldownFromEntity = GetBufferFromEntity<LaunchPadCooldown>()
			}.ScheduleSingle(m_LaunchPadQuery, JobHandle.CombineDependencies(inputDeps, dependency));

			m_TargetImpulseEventProvider.AddJobHandleForProducer(inputDeps);

			return inputDeps;
		}
	}
}