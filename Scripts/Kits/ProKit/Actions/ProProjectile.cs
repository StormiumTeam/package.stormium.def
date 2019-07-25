using System;
using Runtime.Systems;
using Runtime.Systems.Filters;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using static Unity.Mathematics.math;
using float3 = Unity.Mathematics.float3;
using quaternion = Unity.Mathematics.quaternion;
using Ray = Unity.Physics.Ray;
using RaycastHit = Unity.Physics.RaycastHit;
using SphereCollider = Unity.Physics.SphereCollider;

namespace Stormium.Default.Kits.ProKit
{
	public enum StandardProjectilePhase : byte
	{
		None   = 0,
		Active = 1,
		Ended  = 2
	}

	public enum StandardProjectileEndType : byte
	{
		Disabled  = 0,
		Lifetime  = 1,
		Collision = 2
	}

	public struct ProProjectile
	{
		public static readonly ComponentType[] ProviderBasicComponents =
		{
			ComponentType.ReadWrite<ProjectileDescription>(),
			ComponentType.ReadWrite<Translation>(),
			ComponentType.ReadWrite<Rotation>(),
			ComponentType.ReadWrite<LocalToWorld>(),
			ComponentType.ReadWrite<TransformState>(),
			ComponentType.ReadWrite<TransformStateDirection>(),
			ComponentType.ReadWrite<ProProjectile.Settings>(),
			ComponentType.ReadWrite<ProProjectile.PredictedState>(),
			ComponentType.ReadWrite<Velocity>(),
			ComponentType.ReadWrite<CollideWith>(),
			ComponentType.ReadWrite<GenerateEntitySnapshot>()
		};

		[Serializable]
		public struct PredictedState : IComponentData, IInterpolatable<PredictedState>, IPredictable<PredictedState>
		{
			public StandardProjectilePhase   phase;
			public StandardProjectileEndType endType;
			public int                       endTick;
			public int                       hitTick;
			public int                       bounce;
			public float3                    explodeNormalHit;
			
			public bool startDamageEvent;

			public bool VerifyPrediction(in PredictedState real)
			{
				return phase >= real.phase
				       && real.endTick - endTick <= 50 && real.endTick - endTick >= 0
				       && bounce - real.bounce <= 1 && bounce - real.bounce >= 0;
			}

			public void Interpolate(in PredictedState next, float progress)
			{
				phase   = next.phase;
				endTick = (int) lerp(endTick, next.endTick, progress);
				bounce  = next.bounce;
			}
		}

		[Serializable]
		public struct Settings : IComponentData
		{
			public float detectRadius;

			public int    maxBounce;
			public float3 bounciness;

			public float damageRadius;
			public int   damage;

			public float  bumpRadius;
			public float3 bumpForce;

			public float3 gravity;

			public bool manualPhysicSimulation;
			public bool manualEventSimulation;
		}
	}

	[UpdateInGroup(typeof(ProProjectileSystemGroup))]
	public class ProProjectileProcessSystemGroup : ComponentSystemGroup
	{
	}

	[UpdateInGroup(typeof(ProProjectileSystemGroup))]
	public class ProProjectilePhysicsSystemGroup : ComponentSystemGroup
	{
	}

	[UpdateInGroup(typeof(ProProjectileSystemGroup))]
	public class ProProjectileEventSystemGroup : ComponentSystemGroup
	{
	}

	[UpdateInGroup(typeof(ProProjectileProcessSystemGroup))]
	public class ProProjectileProcessSystem : GameBaseSystem
	{
		[BurstCompile]
		private struct Job : IJobForEachWithEntity<ProProjectile.Settings, ProProjectile.PredictedState, Translation, Velocity>
		{
			public GameTimeComponent              Time;
			public EntityCommandBuffer.Concurrent Ecb;

			public void Execute(Entity entity, int idx, [ReadOnly] ref ProProjectile.Settings settings, ref ProProjectile.PredictedState state, ref Translation translation, ref Velocity velocity)
			{
				if (state.phase == StandardProjectilePhase.Ended)
				{
					// We give a small delay so clients can receive the explode effect
					if (state.endTick + 1000 > Time.Tick)
						return;

					Ecb.DestroyEntity(idx, entity);
					return;
				}

				if (settings.manualPhysicSimulation)
					return;

				velocity.Value += settings.gravity * Time.DeltaTime;
			}
		}

		protected override void OnCreate()
		{
			base.OnCreate();

			// we need one without the CW buffer, so this system can still run
			GetEntityQuery(typeof(ProProjectile.Settings), typeof(ProProjectile.PredictedState), typeof(Translation), typeof(Velocity));
		}

		protected override void OnUpdate()
		{
			var job = new Job
			{
				Time = GetSingleton<GameTimeComponent>(),
				Ecb  = PostUpdateCommands.ToConcurrent()
			};

			job.Run(this);
		}
	}

	[UpdateInGroup(typeof(ProProjectilePhysicsSystemGroup))]
	public unsafe class ProProjectilePhysicsSystem : GameBaseSystem
	{
		[BurstCompile]
		struct Job : IJobForEachWithEntity<ProProjectile.Settings, ProProjectile.PredictedState, Translation, Velocity>
		{
			public GameTimeComponent Time;

			public JobPhysicsQuery SphereQuery;

			[NativeDisableParallelForRestriction]
			public BufferFromEntity<CollideWith> CollideWithFromEntity;

			[NativeDisableParallelForRestriction, ReadOnly]
			public ComponentDataFromEntity<EnvironmentTag> EnvironmentTagFromEntity;

			public PhysicsWorld PhysicsWorld;

			public EntityCommandBuffer.Concurrent Ecb;

			public void Execute(Entity entity, int idx, [ReadOnly] ref ProProjectile.Settings settings, ref ProProjectile.PredictedState state, ref Translation translation, ref Velocity velocity)
			{
				if (state.phase != StandardProjectilePhase.Active || settings.manualPhysicSimulation)
					return;
					
				var targetPosition = translation.Value + velocity.Value * Time.DeltaTime;

				// first, make a standard check on static collider / rigidBodies
				var castInput = new ColliderCastInput
				{
					Collider    = SphereQuery.Ptr,
					Position    = translation.Value,
					Direction   = velocity.Value * Time.DeltaTime,
					Orientation = quaternion.identity
				};

				var sphereCollider = (SphereCollider*) SphereQuery.Ptr;
				sphereCollider->Radius = max(settings.detectRadius, 0.0001f);
				sphereCollider->Filter = new CollisionFilter
				{
					GroupIndex   = 0,
					CategoryBits = CollisionFilter.Default.CategoryBits,
					MaskBits     = CollisionFilter.Default.MaskBits
				};
				NativeArray<int> filter = default;
				if (CollideWithFromEntity.Exists(entity))
				{
					filter = CollideWithFromEntity[entity].Reinterpret<int>().AsNativeArray();
				}

				var closestHitCollector = new ClosestHitCollector<ColliderCastHit>(1.0f);
				for (var i = 0; i != filter.Length; i++)
				{
					var key       = filter[i];
					var rigidBody = PhysicsWorld.Bodies[key];

					var collection = new CustomCollideCollection(new CustomCollide(rigidBody));
					if (!collection.CastCollider(castInput, ref closestHitCollector))
						continue;

					targetPosition         = closestHitCollector.ClosestHit.Position;
					state.hitTick          = Time.Tick;
					state.explodeNormalHit = closestHitCollector.ClosestHit.SurfaceNormal;

					var hit           = closestHitCollector.ClosestHit;
					var hitEntity     = rigidBody.Entity;
					var isEnvironment = EnvironmentTagFromEntity.Exists(hitEntity);

					// bounce on environment
					// if we are doing bouncing, we don't want projectiles to be inside of meshes (or else, it will break)
					if (isEnvironment && settings.maxBounce > 0 && lengthsq(hit.SurfaceNormal) > 0 && hit.Fraction >= 0.0f)
					{
						velocity.Value =  reflect(velocity.Value, hit.SurfaceNormal) * settings.bounciness;
						targetPosition += hit.SurfaceNormal * (sphereCollider->Radius);

						state.bounce++;
					}
					else
					{
						state.bounce = settings.maxBounce;
					}

					if (settings.maxBounce == 0 || (settings.maxBounce > 0 && state.bounce >= settings.maxBounce))
					{
						// no matter what, we need to set the proj pros to the hit point.
						targetPosition = hit.Position;

						state.phase   = StandardProjectilePhase.Ended;
						state.endType = StandardProjectileEndType.Collision;

						state.endTick = Time.Tick;
					}

					break;
				}

				if (state.phase == StandardProjectilePhase.Active && state.endTick > 0 && state.endTick <= Time.Tick)
				{
					state.phase   = StandardProjectilePhase.Ended;
					state.endType = StandardProjectileEndType.Lifetime;
				}

				if (state.phase == StandardProjectilePhase.Ended)
					state.startDamageEvent = true;
				
				translation.Value = targetPosition;
			}
		}

		public JobPhysicsQuery SphereQuery;
		public EntityQuery     FilterQuery;

		protected override void OnCreate()
		{
			base.OnCreate();
			
			SphereQuery = new JobPhysicsQuery(() => SphereCollider.Create(0, 0.01f, CollisionFilter.Default));
			FilterQuery = GetEntityQuery(typeof(ProProjectile.Settings), typeof(ProProjectile.PredictedState), typeof(Velocity), typeof(Translation), typeof(CollideWith));
			
			// one without CW..
			GetEntityQuery(typeof(ProProjectile.Settings), typeof(ProProjectile.PredictedState), typeof(Velocity), typeof(Translation));
		}

		protected override void OnUpdate()
		{
			World.GetExistingSystem<CollisionFilterSystemGroup>().Filter(FilterQuery);

			var job = new Job
			{
				Time                     = GetSingleton<GameTimeComponent>(),
				CollideWithFromEntity    = GetBufferFromEntity<CollideWith>(),
				EnvironmentTagFromEntity = GetComponentDataFromEntity<EnvironmentTag>(),
				PhysicsWorld             = World.GetExistingSystem<BuildPhysicsWorld>().PhysicsWorld,
				Ecb                      = PostUpdateCommands.ToConcurrent(),
				SphereQuery              = SphereQuery
			};
			job.Run(this);

			World.GetExistingSystem<ColliderCastEventProvider>().FlushDelayedEntities();
		}
	}

	[UpdateInGroup(typeof(ProProjectileEventSystemGroup))]
	public unsafe class ProProjectileEventSystem : GameBaseSystem
	{
		private struct ChunkPayload
		{
			[ReadOnly] public ArchetypeChunkEntityType                  EntityType;
			public            ArchetypeChunkComponentType<LocalToWorld> LocalToWorldType;

			public ArchetypeChunkComponentType<PhysicsCollider> PhysicsColliderType;
			//public ArchetypeChunkComponentType<PhysicsVelocity> PhysicsVelocityType;
			//public ArchetypeChunkComponentType<Velocity>        LegacyVelocityType;

			//public NativeArray<ArchetypeChunk> MovableChunks;
			public NativeArray<ArchetypeChunk> HitColliderChunks;
		}

		[BurstCompile]
		struct Job : IJobForEachWithEntity<ProProjectile.Settings, ProProjectile.PredictedState, Translation, Velocity>
		{
			public EntityCommandBuffer.Concurrent Ecb;
			public ChunkPayload CPayload;

			public void Execute(Entity entity, int idx, [ReadOnly] ref ProProjectile.Settings settings, ref ProProjectile.PredictedState state, ref Translation translation, ref Velocity velocity)
			{
				if (settings.manualEventSimulation || !state.startDamageEvent)
					return;

				state.startDamageEvent = false;
				
				var rayInput = new RaycastInput
				{
					Filter = CollisionFilter.Default,
					Ray    = new Ray {Origin = translation.Value}
				};
				var closestRayHit = new ClosestHitCollector<RaycastHit>(1.0f);

				for (var i = 0; settings.damageRadius > 0.0f && i != CPayload.HitColliderChunks.Length; i++)
				{
					var hitColliderChunk  = CPayload.HitColliderChunks[i];
					var entityArray       = hitColliderChunk.GetNativeArray(CPayload.EntityType);
					var localToWorldArray = hitColliderChunk.GetNativeArray(CPayload.LocalToWorldType);
					var colliderArray     = hitColliderChunk.GetNativeArray(CPayload.PhysicsColliderType);
					var count             = hitColliderChunk.Count;

					for (var entityIndex = 0; entityIndex != count; entityIndex++)
					{
						var localToWorld = localToWorldArray[entityIndex];
						var collider     = colliderArray[entityIndex];

						rayInput.Ray.Direction = normalizesafe(localToWorld.Position - rayInput.Ray.Origin) * settings.damageRadius;
						
						var collection = new CustomCollideCollection(new CustomCollide(collider, localToWorld));
						if (!collection.CastRay(rayInput, ref closestRayHit))
							continue;
						
						// if you want to redirect damage to another entity, you can add a HealthRedirection health type to this entity.
						var toDamage = entityArray[entityIndex];

						var dmgEv = Ecb.CreateEntity(idx);

						Ecb.AddComponent(idx, dmgEv, new GameEvent());
						Ecb.AddComponent(idx, dmgEv, new TargetDamageEvent {DmgValue = -settings.damage, Shooter = entity, Victim = toDamage});
					}
				}

				/*for (var i = 0; i != LivableChunks.Length; i++)
				{
					var livableChunk = LivableChunks[i];
					var localToWorldArray = livableChunk.GetNativeArray<LocalToWorld>();
					var physicsVelocityArray = livableChunk.GetNativeArray<PhysicsVelocity>();
					var legacyVelocityArray = livableChunk.GetNativeArray<Velocity>();
					var count = livableChunk.Count;

					for (var entityIndex = 0; entityIndex != count; entityIndex++)
					{
						var position = localToWorldArray[entityIndex].Position;
						ref var pv = ref UnsafeUtilityEx.ArrayElementAsRef<PhysicsVelocity>(physicsVelocityArray.GetUnsafePtr(), entityIndex);
						ref var lv = ref UnsafeUtilityEx.ArrayElementAsRef<Velocity>(legacyVelocityArray.GetUnsafePtr(), entityIndex);
						
						if (livableChunk.Has<PhysicsVelocity>())
						{
							pv.Linear -= ;
						}

						if (livableChunk.Has<Velocity>())
						{
							lv.Value -= ;
						}
					}
				}*/
			}
		}

		public EntityQuery     HitColliderQuery;

		protected override void OnCreate()
		{
			base.OnCreate();
			
			HitColliderQuery = GetEntityQuery(typeof(LocalToWorld), typeof(PhysicsCollider), typeof(HealthContainer));
		}

		protected override void OnUpdate()
		{
			var job = new Job
			{
				Ecb                      = PostUpdateCommands.ToConcurrent(),
				CPayload = new ChunkPayload
				{
					HitColliderChunks   = HitColliderQuery.CreateArchetypeChunkArray(Allocator.TempJob),
					EntityType          = GetArchetypeChunkEntityType(),
					LocalToWorldType    = GetArchetypeChunkComponentType<LocalToWorld>(),
					PhysicsColliderType = GetArchetypeChunkComponentType<PhysicsCollider>()
				}
			};
			job.Run(this);
		}
	}
}