using System;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Filters;
using StormiumTeam.GameBase.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;
using Collider = Unity.Physics.Collider;
using float3 = Unity.Mathematics.float3;
using quaternion = Unity.Mathematics.quaternion;
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
			ComponentType.ReadWrite<ProProjectile.Settings>(),
			ComponentType.ReadWrite<ProProjectile.PredictedState>(),
			ComponentType.ReadWrite<Velocity>(),
			ComponentType.ReadWrite<CollideWith>()
		};

		[Serializable]
		public struct PredictedState : IComponentData
		{
			public StandardProjectilePhase   phase;
			public StandardProjectileEndType endType;
			public UTick                     endTick;
			public UTick                     hitTick;
			public int                       bounce;
			public float3                    explodeNormalHit;

			public bool startDamageEvent;
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

	[UpdateInGroup(typeof(ProjectileSystemGroup))]
	[UpdateBefore(typeof(ProjectilePhysicIterationSystemGroup))]
	public class ProProjectileProcessSystemGroup : ComponentSystemGroup
	{
	}

	[UpdateInGroup(typeof(ProjectilePhysicIterationSystemGroup))]
	public class ProProjectilePhysicsSystemGroup : ComponentSystemGroup
	{
	}

	[UpdateInGroup(typeof(ProjectilePhysicCollisionEventSystemGroup))]
	public class ProProjectileEventSystemGroup : ComponentSystemGroup
	{
	}

	[UpdateInGroup(typeof(ProProjectileProcessSystemGroup))]
	public class ProProjectileProcessSystem : JobGameBaseSystem
	{
		[BurstCompile]
		private struct Job : IJobForEachWithEntity<ProProjectile.Settings, ProProjectile.PredictedState, Translation, Velocity>
		{
			public UTick                          Tick;
			public EntityCommandBuffer.Concurrent Ecb;

			public void Execute(Entity entity, int idx, [ReadOnly] ref ProProjectile.Settings settings, ref ProProjectile.PredictedState state, ref Translation translation, ref Velocity velocity)
			{
				if (state.phase == StandardProjectilePhase.Ended)
				{
					// We give a small delay so clients can receive the explode effect
					if (UTick.MsToTickNextFrame(state.endTick, 1000) > Tick)
						return;

					Ecb.DestroyEntity(idx, entity);
					return;
				}

				if (settings.manualPhysicSimulation)
					return;

				velocity.Value += settings.gravity * Tick.Delta;
			}
		}

		private EntityQuery                            m_Query;
		private EndProjectileEntityCommandBufferSystem m_EndBarrier;

		protected override void OnCreate()
		{
			base.OnCreate();

			// we need one without the CW buffer, so this system can still run
			m_Query      = GetEntityQuery(typeof(ProProjectile.Settings), typeof(ProProjectile.PredictedState), typeof(Translation), typeof(Velocity));
			m_EndBarrier = World.GetOrCreateSystem<EndProjectileEntityCommandBufferSystem>();
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			inputDeps = new Job
			{
				Tick = ServerSimulationSystemGroup.GetTick(),
				Ecb  = m_EndBarrier.CreateCommandBuffer().ToConcurrent()
			}.Schedule(m_Query, inputDeps);

			m_EndBarrier.AddJobHandleForProducer(inputDeps);

			return inputDeps;
		}
	}

	[UpdateInGroup(typeof(ProProjectilePhysicsSystemGroup))]
	public unsafe class ProProjectilePhysicsSystem : JobGameBaseSystem
	{
		[BurstCompile]
		struct Job : IJobForEachWithEntity<ProProjectile.Settings, ProProjectile.PredictedState, Translation, Velocity>
		{
			public UTick Tick;

			[NativeDisableParallelForRestriction]
			public BufferFromEntity<CollideWith> CollideWithFromEntity;

			[NativeDisableParallelForRestriction, ReadOnly]
			public ComponentDataFromEntity<EnvironmentTag> EnvironmentTagFromEntity;

			public PhysicsWorld PhysicsWorld;

			public void Execute(Entity entity, int idx, [ReadOnly] ref ProProjectile.Settings settings, ref ProProjectile.PredictedState state, ref Translation translation, ref Velocity velocity)
			{
				if (state.phase != StandardProjectilePhase.Active || settings.manualPhysicSimulation)
					return;

				var targetPosition = translation.Value + velocity.Value * Tick.Delta;

				// first, make a standard check on static collider / rigidBodies
				var finalScanRadius = max(settings.detectRadius, 0.0001f);
				var scanSphere      = SphereCollider.Create(new SphereGeometry {Radius = finalScanRadius});
				var castInput = new ColliderCastInput
				{
					Collider    = (Collider*) scanSphere.GetUnsafePtr(),
					Start       = translation.Value,
					End         = translation.Value + velocity.Value * Tick.Delta,
					Orientation = quaternion.identity
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
					state.hitTick          = Tick;
					state.explodeNormalHit = closestHitCollector.ClosestHit.SurfaceNormal;

					var hit           = closestHitCollector.ClosestHit;
					var hitEntity     = rigidBody.Entity;
					var isEnvironment = EnvironmentTagFromEntity.Exists(hitEntity);

					// bounce on environment
					// if we are doing bouncing, we don't want projectiles to be inside of meshes (or else, it will break)
					if (isEnvironment && settings.maxBounce > 0 && lengthsq(hit.SurfaceNormal) > 0 && hit.Fraction >= 0.0f)
					{
						velocity.Value =  reflect(velocity.Value, hit.SurfaceNormal) * settings.bounciness;
						targetPosition += hit.SurfaceNormal * finalScanRadius;

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

						state.endTick = Tick;
					}

					break;
				}

				if (state.phase == StandardProjectilePhase.Active && state.endTick > 0 && state.endTick <= Tick)
				{
					state.phase   = StandardProjectilePhase.Ended;
					state.endType = StandardProjectileEndType.Lifetime;
				}

				if (state.phase == StandardProjectilePhase.Ended)
					state.startDamageEvent = true;

				translation.Value = targetPosition;
				scanSphere.Dispose();
			}
		}

		public EntityQuery     FilterQuery;
		public EntityQuery     ProjectileQuery;

		protected override void OnCreate()
		{
			base.OnCreate();

			FilterQuery = GetEntityQuery(typeof(ProProjectile.Settings), typeof(ProProjectile.PredictedState), typeof(Velocity), typeof(Translation), typeof(CollideWith));

			// one without CW..
			ProjectileQuery = GetEntityQuery(typeof(ProProjectile.Settings), typeof(ProProjectile.PredictedState), typeof(Velocity), typeof(Translation));
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			inputDeps = World.GetExistingSystem<CollisionFilterSystemGroup>().Filter(FilterQuery, inputDeps);
			inputDeps = JobForEachExtensions.Schedule(new Job
			{
				Tick                     = ServerSimulationSystemGroup.GetTick(),
				CollideWithFromEntity    = GetBufferFromEntity<CollideWith>(),
				EnvironmentTagFromEntity = GetComponentDataFromEntity<EnvironmentTag>(),
				PhysicsWorld             = World.GetExistingSystem<BuildPhysicsWorld>().PhysicsWorld,
			}, ProjectileQuery, inputDeps);

			return inputDeps;
		}
	}

	[UpdateInGroup(typeof(ProProjectileEventSystemGroup))]
	public unsafe class ProProjectileEventSystem : JobGameBaseSystem
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
		private struct Job : IJobForEachWithEntity<ProProjectile.Settings, ProProjectile.PredictedState, Translation, Velocity>
		{
			public NativeList<TargetDamageEvent> DamageEventList;
			public ChunkPayload CPayload;

			public void Execute(Entity entity, int idx, [ReadOnly] ref ProProjectile.Settings settings, ref ProProjectile.PredictedState state, ref Translation translation, ref Velocity velocity)
			{
				if (settings.manualEventSimulation || !state.startDamageEvent)
					return;

				state.startDamageEvent = false;

				var rayInput = new RaycastInput
				{
					Filter = CollisionFilter.Default,
					Start  = translation.Value
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

						var dir = normalizesafe(localToWorld.Position - rayInput.Start);
						rayInput.End = translation.Value + dir * settings.damageRadius;
						// ^ this feels awkward, maybe we could do a collider cast instead?....

						var collection = new CustomCollideCollection(new CustomCollide(collider, localToWorld));
						if (!collection.CastRay(rayInput, ref closestRayHit))
							continue;

						// if you want to redirect damage to another entity, you can add a HealthRedirection health type to this entity.
						var toDamage = entityArray[entityIndex];
						DamageEventList.Add(new TargetDamageEvent
						{
							Damage = -settings.damage, 
							Origin = entity,
							Destination = toDamage
						});
					}
				}
			}
		}

		public EntityQuery     HitColliderQuery;
		private TargetDamageEvent.Provider m_DamageEventProvider;
		
		protected override void OnCreate()
		{
			base.OnCreate();
			
			HitColliderQuery = GetEntityQuery(typeof(LocalToWorld), typeof(PhysicsCollider), typeof(HealthContainer));
			m_DamageEventProvider = World.GetOrCreateSystem<TargetDamageEvent.Provider>();
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			inputDeps = new Job
			{
				DamageEventList = m_DamageEventProvider.GetEntityDelayedList(),
				CPayload = new ChunkPayload
				{
					HitColliderChunks   = HitColliderQuery.CreateArchetypeChunkArray(Allocator.TempJob),
					EntityType          = GetArchetypeChunkEntityType(),
					LocalToWorldType    = GetArchetypeChunkComponentType<LocalToWorld>(),
					PhysicsColliderType = GetArchetypeChunkComponentType<PhysicsCollider>()
				}	
			}.ScheduleSingle(this, inputDeps);
			
			m_DamageEventProvider.AddJobHandleForProducer(inputDeps);

			return inputDeps;
		}
	}
}