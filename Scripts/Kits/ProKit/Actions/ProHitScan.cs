using System;
using StormiumTeam.GameBase;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using quaternion = Unity.Mathematics.quaternion;

namespace Stormium.Default.Kits.ProKit
{
	public struct ProHitScan
	{
		[Serializable]
		public struct PredictedState : IComponentData, IInterpolatable<PredictedState>, IPredictable<PredictedState>
		{
			public StandardProjectilePhase phase;
			public int                     explodeTick;
			public int                     bounce;
			public float3                  explodePosition;

			public bool VerifyPrediction(in PredictedState real)
			{
				return phase >= real.phase
				       && real.explodeTick - explodeTick <= 50 && real.explodeTick - explodeTick >= 0
				       && bounce - real.bounce <= 1 && bounce - real.bounce >= 0;
			}

			public void Interpolate(in PredictedState next, float progress)
			{
				phase       = next.phase;
				explodeTick = (int) lerp(explodeTick, next.explodeTick, progress);
				bounce      = next.bounce;
			}
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
	public class ProHitScanSystem : GameBaseSystem
	{
		private EntityQueryBuilder                  m_ProjectileQuery;
		private ProProjectileExplosionEventProvider m_ExplosionEventProvider;

		private EntityQueryBuilder.F_EDDDD<ProProjectile.Settings, ProProjectile.PredictedState, Translation, Velocity> m_PhysicIteration;

		[BurstCompile]
		private unsafe struct JobPhysicIteration : IJobForEachWithEntity<ProHitScan.Settings, ProHitScan.PredictedState, Translation, Velocity>
		{
			public GameTime            Time;
			public EntityCommandBuffer Ecb;

			public JobPhysicsQuery JobSphereScanCast;
			public JobPhysicsQuery JobSphereDamageCast;
			public JobPhysicsQuery JobSphereBumpCast;

			public BufferFromEntity<CustomCollide> CwBufferFromEntity;

			[WriteOnly] public NativeList<ProProjectileExplosionEventProvider.CreateDelayedData> ProviderCreateEntityDelayed;

			// should have a OwnerState<LivableDescription> at least...
			[DeallocateOnJobCompletion] public NativeArray<ArchetypeChunk>                  CollideLivableChunks;
			[ReadOnly] public ArchetypeChunkEntityType                     EntityType;
			[ReadOnly] public ArchetypeChunkComponentType<LocalToWorld>    LocalToWorldType;
			[ReadOnly] public ArchetypeChunkComponentType<PhysicsCollider> PhysicsColliderType;

			public void Execute(Entity                        entity, int index,
			                    ref ProHitScan.Settings       settings,
			                    ref ProHitScan.PredictedState state,
			                    ref Translation               translation,
			                    ref Velocity                  velocity)
			{
				if (state.phase != StandardProjectilePhase.Active)
				{
					// We give a small delay so clients can receive the explode effect
					if (state.explodeTick + 1000 > Time.Tick)
						return;

					Ecb.DestroyEntity(entity);
					return;
				}

				// we prepare the data for the events (instead of recreating it every time...)
				var delayedData = new ProProjectileExplosionEventProvider.CreateDelayedData
				{
					BumpEvent = new TargetBumpEvent
					{
						Shooter       = entity,
						Force         = settings.bumpForce,
						Position      = translation.Value,
						VelocityReset = float3(1)
					},
					DamageEvent = new TargetDamageEvent
					{
						Shooter  = entity,
						DmgValue = settings.damage
					}
				};

				// we prepare the sphere cast for the scanning
				var sphereCollider = (SphereCollider*) JobSphereScanCast.Ptr;
				sphereCollider->Radius = max(settings.scanRadius, 0.01f);

				var scanning = new ColliderCastInput
				{
					Collider    = (Collider*) sphereCollider,
					Direction   = normalizesafe(velocity.Value) * settings.scanDistance,
					Orientation = quaternion.identity,
					Position    = translation.Value
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
				state.explodeTick = Time.Tick;
				state.phase       = StandardProjectilePhase.Ended;

				if (hitEntity == default)
					return;

				var scanLivableCast = new ColliderCastInput
				{
					Position = translation.Value,
					Orientation = quaternion.identity,
					Direction = hitInfo.Position - translation.Value 
				};

				// prepare cast sphere first....
				var damageSphere = (SphereCollider*) JobSphereDamageCast.Ptr;
				damageSphere->Radius = max(settings.damage, 0.0f);

				var bumpSphere = (SphereCollider*) JobSphereBumpCast.Ptr;
				bumpSphere->Radius = max(settings.bumpRadius, 0.0f);

				// prepare the distance inputs...
				var damageCastInput = scanLivableCast;
				damageCastInput.Collider = (Collider*) damageSphere;

				var bumpCastInput = scanLivableCast;
				bumpCastInput.Collider = (Collider*) bumpSphere;

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

						delayedData.HasDamageEvent = false;
						delayedData.HasBumpEvent   = false;

						var damageCastCollSpace = CastHelper.TransformSpace(damageCastInput, new RigidTransform(localToWorld.Value), out _);
						var bumpCastCollSpace   = CastHelper.TransformSpace(bumpCastInput, new RigidTransform(localToWorld.Value), out var bWorldFromMotion);

						// this struct is a bit bugged, hence why 1.00001f ¯\_(ツ)_/¯
						var anyHitCollector = new AnyHitCollector<ColliderCastHit>(1.00001f);
						if (physicsCollider.ColliderPtr->CastCollider(damageCastCollSpace, ref anyHitCollector))
						{
							delayedData.HasDamageEvent     = true;
							delayedData.DamageEvent.Victim = collideEntity;
						}

						if (physicsCollider.ColliderPtr->CastCollider(bumpCastCollSpace, out var closestHit))
						{
							closestHit.Transform(bWorldFromMotion, -1);

							delayedData.HasBumpEvent        = true;
							delayedData.BumpEvent.Victim    = collideEntity;
							delayedData.BumpEvent.Direction = normalizesafe(closestHit.Position - translation.Value);
						}

						if (delayedData.HasDamageEvent || delayedData.HasBumpEvent)
							ProviderCreateEntityDelayed.Add(delayedData);
					}
				}

				enumerator.Dispose();
			}
		}

		private JobPhysicsQuery m_JobSphereScanCast, m_JobSphereDamageCast, m_JobSphereBumpCast;
		private EntityQuery  m_ProjectileGroup;
		private EntityQuery m_CollideLivableGroup;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_JobSphereScanCast   = new JobPhysicsQuery(() => SphereCollider.Create(float3(0), 0.1f));
			m_JobSphereDamageCast = new JobPhysicsQuery(() => SphereCollider.Create(float3(0), 0.1f));
			m_JobSphereBumpCast   = new JobPhysicsQuery(() => SphereCollider.Create(float3(0), 0.1f));

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
						ComponentType.ReadOnly<ColliderDescription>(),
						ComponentType.ReadOnly<Owner>(),
						ComponentType.ReadOnly<LocalToWorld>(),
						ComponentType.ReadOnly<PhysicsCollider>(),
					}
				}
			);
		}

		protected override void OnUpdate()
		{
			World.GetExistingSystem<TransformCustomCollideBufferSystem>().ScheduleJob(default)
			     .Complete();
			
			var collideLivableChunks = m_CollideLivableGroup.CreateArchetypeChunkArray(Allocator.TempJob);

			var groupEcb = World.GetOrCreateSystem<ProjectileSystemGroup>().PostUpdateCommands;
			var job = new JobPhysicIteration
			{
				Time                = GetSingleton<GameTimeComponent>().ToGameTime(),
				CwBufferFromEntity  = GetBufferFromEntity<CustomCollide>(),
				Ecb                 = groupEcb,
				JobSphereScanCast   = m_JobSphereScanCast,
				JobSphereDamageCast = m_JobSphereDamageCast,
				JobSphereBumpCast   = m_JobSphereBumpCast,

				ProviderCreateEntityDelayed = m_ExplosionEventProvider.GetEntityDelayedList(),

				CollideLivableChunks = collideLivableChunks,
				EntityType           = GetArchetypeChunkEntityType(),
				LocalToWorldType     = GetArchetypeChunkComponentType<LocalToWorld>(true),
				PhysicsColliderType  = GetArchetypeChunkComponentType<PhysicsCollider>(true),
			};

			job.Run(m_ProjectileGroup);
		}
	}
}