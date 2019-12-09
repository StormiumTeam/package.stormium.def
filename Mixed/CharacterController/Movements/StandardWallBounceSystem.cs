using package.stormium.def;
using package.stormiumteam.shared.ecs;
using Stormium.Default;
using StormiumTeam.GameBase;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using Collider = Unity.Physics.Collider;

namespace CharacterController
{
	[UpdateInGroup(typeof(CharacterMovementGroup))]
	[UpdateAfter(typeof(CharacterMovementInitSystem))]
	public class StandardWallBounceSystem : JobGameBaseSystem
	{
		[BurstCompile]
		unsafe struct ChunkJob : IJobChunk
		{
			[ReadOnly] public UTick        Tick;
			[ReadOnly] public PhysicsWorld PhysicsWorld;

			[ReadOnly] public ArchetypeChunkEntityType                    EntityType;
			[ReadOnly] public ArchetypeChunkComponentType<CharacterInput> CharacterInputType;
			[ReadOnly] public ArchetypeChunkComponentType<AirTime>        AirTimeType;

			public ArchetypeChunkBufferType<CharacterPass> CharacterPassType;

			public ArchetypeChunkComponentType<StandardJumpMovement>  JumpMovementType;
			public ArchetypeChunkComponentType<StandardDodgeMovement> DodgeMovementType;
			public ArchetypeChunkComponentType<StandardWallBounce>    WallBounceMovementType;
			public ArchetypeChunkComponentType<StandardAerialMovement> AerialMovementType;

			public ArchetypeChunkComponentType<Velocity>    VelocityType;
			public ArchetypeChunkComponentType<Translation> TranslationType;
			public ArchetypeChunkComponentType<Stamina>     StaminaType;

			private static ref T r<T>(NativeArray<T> arr, int i) where T : struct => ref UnsafeUtilityEx.ArrayElementAsRef<T>(arr.GetUnsafePtr(), i);

			public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
			{
				var inputArray            = chunk.GetNativeArray(CharacterInputType);
				var airTimeArray          = chunk.GetNativeArray(AirTimeType);
				var characterPassAccessor = chunk.GetBufferAccessor(CharacterPassType);
				var jumpArray             = chunk.GetNativeArray(JumpMovementType);
				var dodgeArray            = chunk.GetNativeArray(DodgeMovementType);
				var wallBounceArray       = chunk.GetNativeArray(WallBounceMovementType);
				var aerialArray           = chunk.GetNativeArray(AerialMovementType);
				var velocityArray         = chunk.GetNativeArray(VelocityType);
				var translationArray      = chunk.GetNativeArray(TranslationType);
				var staminaArray          = chunk.GetNativeArray(StaminaType);
				for (var ent = 0; ent != chunk.Count; ent++)
				{
					Execute(inputArray[ent], airTimeArray[ent], characterPassAccessor[ent],
						ref r(jumpArray, ent), ref r(dodgeArray, ent), ref r(wallBounceArray, ent),
						ref r(aerialArray, ent),
						ref r(velocityArray, ent), ref r(translationArray, ent), ref r(staminaArray, ent));
				}
			}

			private void Execute(in  CharacterInput         input,         in  AirTime               airTime,        DynamicBuffer<CharacterPass> passes,
			                     ref StandardJumpMovement   jumpComponent, ref StandardDodgeMovement dodgeComponent, ref StandardWallBounce       wallBounceComponent,
			                     ref StandardAerialMovement aerialComponent,
			                     ref Velocity               velocity, ref Translation translation, ref Stamina stamina)
			{
				if (!passes.TryGetPass(passes.Length - 1, out var current))
					return;
				
				if ((current.Ground.State & GroundState.TouchGround) != 0)
					return;

				var moveData         = current.ToMoveData();
				var directionForward = SrtMovement.ComputeDirectionFwd(current.ToWorld.Forward, current.Rotation, input.Move);

				// ------------------- ------------------- -------------------
				// Wall Bouncing
				if (airTime.Value > 0.075f && CanWallBounce(ref moveData, jumpComponent, dodgeComponent, wallBounceComponent, current.Direction, out var closestHit))
				{
					if (jumpComponent.JumpQueued >= Tick && wallBounceComponent.EnableWallJump)
					{
						jumpComponent.JumpQueued = default;

						var bouncePower = 6f;
						if (!stamina.HasEnough(wallBounceComponent.StaminaUsageOnWallJump, out var neededPercentage))
						{
							bouncePower *= neededPercentage;
						}

						var bounce        = closestHit.SurfaceNormal * bouncePower;
						var verticalBonus = math.distance(math.min(velocity.Value.y, 0), math.min(velocity.Value.y + 4f, 0));
						bounce.y += bouncePower + verticalBonus;

						velocity.Value =  RayUtility.SlideVelocityNoYChange(velocity.Value, closestHit.SurfaceNormal);
						velocity.Value += bounce;

						aerialComponent.AirControl *= 0.5f;

						stamina.Apply(wallBounceComponent.StaminaUsageOnWallJump);
					}
					else if (dodgeComponent.DodgeQueued >= Tick && wallBounceComponent.EnableWallDodge)
					{
						dodgeComponent.DodgeQueued = default;

						var power = 1f;
						if (!stamina.HasEnough(wallBounceComponent.StaminaUsageOnWallDodge, out var neededPercentage))
						{
							power *= neededPercentage;
						}

						var oldY       = velocity.Value.y;
						var dirInertia = RayUtility.SlideVelocityNoYChange(math.normalizesafe(velocity.xfz), closestHit.SurfaceNormal);
						var speed      = math.clamp(math.length(velocity.Value.xz) + 2.5f, 12.5f, 16f);

						var choice0 = closestHit.SurfaceNormal;

						var dotProduct = math.dot(directionForward, dirInertia);
						if (dotProduct < 0)
						{
							directionForward = SrtMovement.ComputeDirectionFwd(current.ToWorld.Forward, current.Rotation, input.Move * -1);
						}

						var normalPower = 0.325f; // range between [0..1] (reco: 0.5)
						var wantedVelocity = (float3) Vector3.Lerp(Vector3.Reflect(directionForward, choice0), choice0, normalPower) * speed;
						wantedVelocity   += closestHit.SurfaceNormal * math.abs(dotProduct) * 3.5f;
						wantedVelocity.y =  math.max(oldY + 2f, 2.5f);

						velocity.Value = math.lerp(velocity.Value, wantedVelocity, power);

						moveData.Position += velocity.Value * 0.1f;
						PhysicsCharacter.Depenetrate(ref moveData, PhysicsWorld);

						aerialComponent.AirControl *= 0.25f;

						stamina.Apply(wallBounceComponent.StaminaUsageOnWallDodge);
					}

					wallBounceComponent.LastBounceTick = Tick;
				}

				current.Position = moveData.Position;
				current.Velocity = velocity.Value;
				passes.Add(current);
			}

			private bool CanWallBounce(ref MoveData moveData, StandardJumpMovement jump, StandardDodgeMovement dodge, StandardWallBounce wbComponent, float3 direction, out ColliderCastHit closestHit)
			{
				var shouldWallBounce = (jump.JumpQueued >= Tick && wbComponent.EnableWallJump) || (dodge.DodgeQueued >= Tick && wbComponent.EnableWallDodge);
				if (!shouldWallBounce || UTick.AddMsNextFrame(wbComponent.LastBounceTick, 100) >= Tick)
				{
					closestHit = default;
					return false;
				}

				const float surroundingSensibility = 0.2f;
				const float directionSensibility   = 0.25f;

				var probeGeom = moveData.Probe->Geometry;
				probeGeom.Radius         = moveData.Collider->Radius + surroundingSensibility;
				moveData.Probe->Geometry = probeGeom;

				var castInput = new ColliderCastInput
				{
					Collider    = (Collider*) moveData.Probe,
					Orientation = quaternion.identity,
					Start       = moveData.Position,
					End         = moveData.Position + math.normalizesafe(direction) * directionSensibility
				};
				return PhysicsWorld.CastCollider(castInput, out closestHit) && math.abs(closestHit.SurfaceNormal.y) <= 0.1f;
			}
		}

		private LazySystem<BuildPhysicsWorld> m_BuildWorldSystem;
		private EntityQuery                   m_Query;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_Query = GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[]
				{
					typeof(CharacterPass),
					typeof(CharacterInput),
					typeof(AirTime),
					typeof(Translation),
					typeof(Velocity),
					typeof(Stamina),
					typeof(StandardJumpMovement),
					typeof(StandardDodgeMovement),
					typeof(StandardWallBounce),
					typeof(StandardAerialMovement),
				},
				None = new ComponentType[] {typeof(IgnoreCharacterMovement)}
			});
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			return new ChunkJob
			{
				Tick         = GetTick(true),
				PhysicsWorld = this.L(ref m_BuildWorldSystem).PhysicsWorld,

				EntityType         = GetArchetypeChunkEntityType(),
				CharacterInputType = GetArchetypeChunkComponentType<CharacterInput>(true),
				AirTimeType        = GetArchetypeChunkComponentType<AirTime>(true),

				CharacterPassType      = GetArchetypeChunkBufferType<CharacterPass>(),
				JumpMovementType       = GetArchetypeChunkComponentType<StandardJumpMovement>(),
				DodgeMovementType      = GetArchetypeChunkComponentType<StandardDodgeMovement>(),
				WallBounceMovementType = GetArchetypeChunkComponentType<StandardWallBounce>(),
				AerialMovementType = GetArchetypeChunkComponentType<StandardAerialMovement>(),

				VelocityType    = GetArchetypeChunkComponentType<Velocity>(),
				TranslationType = GetArchetypeChunkComponentType<Translation>(),
				StaminaType     = GetArchetypeChunkComponentType<Stamina>(),
			}.Schedule(m_Query, JobHandle.CombineDependencies(inputDeps, m_BuildWorldSystem.Value.FinalJobHandle));
		}
	}
}