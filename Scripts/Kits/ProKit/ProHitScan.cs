using System;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using quaternion = Unity.Mathematics.quaternion;

namespace Stormium.Default.Kits.ProKit
{
	public struct ProHitScan
	{
		public static readonly ComponentType[] ProviderBasicComponents =
		{
			ComponentType.ReadWrite<ProjectileDescription>(),
			ComponentType.ReadWrite<Translation>(),
			ComponentType.ReadWrite<Rotation>(),
			ComponentType.ReadWrite<LocalToWorld>(),
			ComponentType.ReadWrite<ProHitScan.Settings>(),
			ComponentType.ReadWrite<ProHitScan.PredictedState>(),
			ComponentType.ReadWrite<Velocity>(),
			ComponentType.ReadWrite<CollideWith>()
		};

		[Serializable]
		public struct PredictedState : IComponentData
		{
			public StandardProjectilePhase phase;
			public UTick                   explodeTick;
			public int                     bounce;
			public float3                  explodePosition;
		}

		[Serializable]
		public struct Settings : IComponentData
		{
			public float scanRadius;
			public float scanDistance;

			public int maxBounce;
			public int maxPenetration;

			// even if it's set at 0 or negative, if it scanRadius touched an entity, it will still work
			public float damageRadius;
			public int   damage;

			public float  bumpRadius;
			public float3 bumpForce;
		}
	}

	[DisableAutoCreation]
	public class ProHitScanSystem : JobGameBaseSystem
	{
		private EntityQueryBuilder                                                                                      m_ProjectileQuery;
		private EntityQueryBuilder.F_EDDDD<ProProjectile.Settings, ProProjectile.PredictedState, Translation, Velocity> m_PhysicIteration;

		[BurstCompile]
		private unsafe struct JobPhysicIteration : IJobForEachWithEntity<ProHitScan.Settings, ProHitScan.PredictedState, Translation, Velocity>
		{
			public UTick                          Tick;
			public EntityCommandBuffer.Concurrent Ecb;

			public BufferFromEntity<CustomCollide> CwBufferFromEntity;

			[WriteOnly] public NativeList<TargetDamageEvent>  DamageEventSpawnList;
			[WriteOnly] public NativeList<TargetImpulseEvent> ImpulseEventSpawnList;

			// should have a OwnerState<LivableDescription> at least...
			[DeallocateOnJobCompletion] public NativeArray<ArchetypeChunk>                  CollideLivableChunks;
			[ReadOnly]                  public ArchetypeChunkEntityType                     EntityType;
			[ReadOnly]                  public ArchetypeChunkComponentType<LocalToWorld>    LocalToWorldType;
			[ReadOnly]                  public ArchetypeChunkComponentType<PhysicsCollider> PhysicsColliderType;

			public void Execute(Entity                        entity, int index,
			                    ref ProHitScan.Settings       settings,
			                    ref ProHitScan.PredictedState state,
			                    ref Translation               translation,
			                    ref Velocity                  velocity)
			{
				if (state.phase != StandardProjectilePhase.Active)
				{
					// We give a small delay so clients can receive the explode effect
					if (Tick < UTick.AddMsNextFrame(state.explodeTick, 1000))
						return;

					Ecb.DestroyEntity(index, entity);
					return;
				}

				// we prepare the sphere cast for the scanning
				var scanSphere = SphereCollider.Create(new SphereGeometry {Radius = max(settings.scanRadius, 0.01f)});
				var scanning = new ColliderCastInput
				{
					Collider    = (Collider*) scanSphere.GetUnsafePtr(),
					Orientation = quaternion.identity,
					Start       = translation.Value,
					End         = translation.Value + (normalizesafe(velocity.Value) * settings.scanDistance)
				};

				Entity hitEntity = default;

				var collection = new CustomCollideCollection(CwBufferFromEntity[entity]);
				if (collection.CastCollider(scanning, out var hitInfo))
				{
					state.explodePosition = hitInfo.Position;

					hitEntity = collection.GetElementFromRigidBody(hitInfo.RigidBodyIndex).Target;
				}
				else
				{
					// if we didn't hit someone, instead set NaN (so no need for another field to check if we've hit something or not)
					state.explodePosition = float3(float.NaN);
				}

				// this is a hitscan projectile, so no matter what, it will explode
				state.explodeTick = Tick;
				state.phase       = StandardProjectilePhase.Ended;

				if (hitEntity == default)
					return;

				var scanLivableCast = new ColliderCastInput
				{
					Start       = translation.Value,
					Orientation = quaternion.identity,
					End         = translation.Value + (hitInfo.Position - translation.Value)
				};

				// prepare cast sphere first....
				var damageSphere = SphereCollider.Create(new SphereGeometry {Radius = max(settings.damage, 0.0f)});
				var bumpSphere   = SphereCollider.Create(new SphereGeometry {Radius = max(settings.bumpRadius, 0.0f)});

				// prepare the distance inputs...
				var damageCastInput = scanLivableCast;
				damageCastInput.Collider = (Collider*) damageSphere.GetUnsafePtr();

				var bumpCastInput = scanLivableCast;
				bumpCastInput.Collider = (Collider*) bumpSphere.GetUnsafePtr();

				var enumerator = CollideLivableChunks.GetEnumerator();
				while (enumerator.MoveNext())
				{
					var livableChunk         = enumerator.Current;
					var entityArray          = livableChunk.GetNativeArray(EntityType);
					var localToWorldArray    = livableChunk.GetNativeArray(LocalToWorldType);
					var physicsColliderArray = livableChunk.GetNativeArray(PhysicsColliderType);

					var count = livableChunk.Count;
					for (var i = 0; i != count; i++)
					{
						var collideEntity   = entityArray[i];
						var localToWorld    = localToWorldArray[i];
						var physicsCollider = physicsColliderArray[i];

						var damageCastCollSpace = damageCastInput.TransformSpace(new RigidTransform(localToWorld.Value), out _);
						var bumpCastCollSpace   = bumpCastInput.TransformSpace(new RigidTransform(localToWorld.Value), out var bWorldFromMotion);

						// this struct is a bit bugged, hence why 1.00001f ¯\_(ツ)_/¯
						var anyHitCollector = new AnyHitCollector<ColliderCastHit>(1.00001f);
						if (physicsCollider.ColliderPtr->CastCollider(damageCastCollSpace, ref anyHitCollector))
						{
							DamageEventSpawnList.Add(new TargetDamageEvent
							{
								Origin      = entity,
								Destination = collideEntity,
								Damage      = settings.damage
							});
						}

						if (physicsCollider.ColliderPtr->CastCollider(bumpCastCollSpace, out var closestHit))
						{
							closestHit.Transform(bWorldFromMotion, -1);

							ImpulseEventSpawnList.Add(new TargetImpulseEvent
							{
								Origin      = entity,
								Destination = collideEntity,
								Position    = hitInfo.Position,
								Force       = normalizesafe(hitInfo.Position - translation.Value) * settings.bumpForce,
								Momentum    = 1
							});
						}
					}
				}

				scanSphere.Dispose();
				damageSphere.Dispose();
				bumpSphere.Dispose();
				enumerator.Dispose();
			}
		}

		private EntityQuery     m_ProjectileGroup;
		private EntityQuery     m_CollideLivableGroup;

		private TargetDamageEvent.Provider  m_DamageEventProvider;
		private TargetImpulseEvent.Provider m_ImpulseEventProvider;

		private EndProjectileEntityCommandBufferSystem m_EndBarrier;

		protected override void OnCreate()
		{
			base.OnCreate();
			
			m_ProjectileGroup = GetEntityQuery
			(
				new EntityQueryDesc
				{
					All = new[]
					{
						ComponentType.ReadOnly<ProProjectile.Settings>(),
						ComponentType.ReadWrite<ProProjectile.PredictedState>(),
						ComponentType.ReadOnly<Translation>(),
						ComponentType.ReadOnly<Velocity>(),
						ComponentType.ReadOnly<EntityAuthority>()
					}
				}
			);

			m_CollideLivableGroup = GetEntityQuery
			(
				new EntityQueryDesc
				{
					All = new[]
					{
						ComponentType.ReadOnly<HitShapeDescription>(),
						ComponentType.ReadOnly<Owner>(),
						ComponentType.ReadOnly<LocalToWorld>(),
						ComponentType.ReadOnly<PhysicsCollider>(),
					}
				}
			);

			m_DamageEventProvider  = World.GetOrCreateSystem<TargetDamageEvent.Provider>();
			m_ImpulseEventProvider = World.GetOrCreateSystem<TargetImpulseEvent.Provider>();
			m_EndBarrier           = World.GetOrCreateSystem<EndProjectileEntityCommandBufferSystem>();
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			var collideLivableChunks = m_CollideLivableGroup.CreateArchetypeChunkArray(Allocator.TempJob, out var groupDep);

			var job = new JobPhysicIteration
			{
				Tick                = ServerSimulationSystemGroup.GetTick(),
				CwBufferFromEntity  = GetBufferFromEntity<CustomCollide>(),
				Ecb                 = m_EndBarrier.CreateCommandBuffer().ToConcurrent(),

				DamageEventSpawnList  = m_DamageEventProvider.GetEntityDelayedList(),
				ImpulseEventSpawnList = m_ImpulseEventProvider.GetEntityDelayedList(),

				CollideLivableChunks = collideLivableChunks,
				EntityType           = GetArchetypeChunkEntityType(),
				LocalToWorldType     = GetArchetypeChunkComponentType<LocalToWorld>(true),
				PhysicsColliderType  = GetArchetypeChunkComponentType<PhysicsCollider>(true),
			};

			JobForEachExtensions.Schedule(job, m_ProjectileGroup, JobHandle.CombineDependencies(groupDep, inputDeps));

			m_EndBarrier.AddJobHandleForProducer(inputDeps);
			m_DamageEventProvider.AddJobHandleForProducer(inputDeps);
			m_ImpulseEventProvider.AddJobHandleForProducer(inputDeps);

			return inputDeps;
		}
	}
}