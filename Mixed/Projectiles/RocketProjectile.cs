using System;
using package.stormiumteam.shared.ecs;
using Revolution;
using Unity.NetCode;
using Stormium.Core.Projectiles;
using StormiumTeam.GameBase;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Networking.Transport;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using Collider = Unity.Physics.Collider;
using SphereCollider = Unity.Physics.SphereCollider;

namespace Projectiles
{
	public struct RocketProjectile : IReadWriteComponentSnapshot<RocketProjectile>
	{
		public float DetectionRadius;
		public float ExplosionRadius;

		public struct Create
		{
			public Entity Owner;
			public float3 Position;
			public float3 Direction;
		}

		public class Provider : BaseProviderBatch<Create>
		{
			public override void GetComponents(out ComponentType[] entityComponents)
			{
				entityComponents = new ComponentType[]
				{
					typeof(ProjectileDescription),
					typeof(RocketProjectile),
					typeof(Velocity),
					typeof(Translation),
					typeof(LocalToWorld),

					typeof(ProjectileAgeTime),
					typeof(ProjectileDefaultExplosion),
					typeof(DistanceDamageFallOf),
					typeof(DistanceImpulseFallOf),
					typeof(GhostEntity)
				};
			}

			public override void SetEntityData(Entity entity, Create data)
			{
				if (data.Owner == default)
					throw new ArgumentException(nameof(data.Owner));
				if (entity == default)
					throw new ArgumentException(nameof(entity));

				var tick = GetTick(true);

				EntityManager.ReplaceOwnerData(entity, data.Owner);
				EntityManager.SetComponentData(entity, new Translation {Value                = data.Position});
				EntityManager.SetComponentData(entity, new Velocity {Value                   = data.Direction * 34f});
				EntityManager.SetComponentData(entity, new RocketProjectile {DetectionRadius = 0.1f, ExplosionRadius = 2f});
				EntityManager.SetComponentData(entity, new ProjectileAgeTime {StartMs        = tick.Ms, EndMs        = tick.Ms + 3000});

				EntityManager.SetComponentData(entity, new ProjectileDefaultExplosion
				{
					DamageRadius = 2.25f,
					BumpRadius   = 2.5f,

					MinDamage = 10,
					MaxDamage = 50,

					HorizontalImpulseMin = 2.5f,
					HorizontalImpulseMax = 10f,

					VerticalImpulseMin = 2.5f,
					VerticalImpulseMax = 16f,
					
					SelfImpulseFactor = 0.5f
				});

				var dmgFallOf = EntityManager.GetBuffer<DistanceDamageFallOf>(entity);
				dmgFallOf.Add(DistanceDamageFallOf.FromPercentage(1.0f, 1.0f));
				dmgFallOf.Add(DistanceDamageFallOf.FromPercentage(1.0f, 0.95f));
				dmgFallOf.Add(DistanceDamageFallOf.FromPercentage(0.0f, 0.0f));

				var impFallOf = EntityManager.GetBuffer<DistanceImpulseFallOf>(entity);
				impFallOf.Add(DistanceImpulseFallOf.FromPercentage(1.0f, 1.0f));
				impFallOf.Add(DistanceImpulseFallOf.FromPercentage(1.0f, 0.9f));
				impFallOf.Add(DistanceImpulseFallOf.FromPercentage(0.1f, 0.0f));
			}
		}

		public void WriteTo(DataStreamWriter writer, ref RocketProjectile baseline, DefaultSetup setup, SerializeClientData jobData)
		{
			
		}

		public void ReadFrom(ref DataStreamReader.Context ctx, DataStreamReader reader, ref RocketProjectile baseline, DeserializeClientData jobData)
		{
			
		}
		
		public class Sync : MixedComponentSnapshotSystem<RocketProjectile, DefaultSetup>
		{
			public override ComponentType ExcludeComponent => typeof(Exclude);
		}
		
		public struct Exclude : IComponentData {}
	}

	[UpdateInGroup(typeof(OrderGroup.Simulation.UpdateEntities.Interaction))]
	[UpdateInWorld(UpdateInWorld.TargetWorld.Server)]
	public unsafe class RocketProjectileSystem : JobGameBaseSystem
	{
		[BurstCompile]
		private struct UpdateJob : IJobForEachWithEntity_ECCCC<RocketProjectile, Translation, Velocity, Relative<TeamDescription>>
		{
			public UTick Tick;

			public ComponentType EndedComponentType;
			public ComponentType OutOfTimeComponentType;

			[ReadOnly]
			public PhysicsWorld PhysicsWorld;

			[ReadOnly] public ComponentDataFromEntity<Relative<TeamDescription>> RelativeTeamFromEntity;
			[ReadOnly] public ComponentDataFromEntity<ProjectileAgeTime>         AgeTimeFromEntity;

			public EntityCommandBuffer.Concurrent Ecb;

			int Cast(Entity ent, Entity team, in ColliderCastInput input, out ColliderCastHit hit)
			{
				hit = default;
				
				var rigidBodies       = PhysicsWorld.Bodies;
				var rbCount           = rigidBodies.Length;
				var minFriction       = float.MaxValue;
				var selectedRigidBody = -1;
				for (var i = 0; i < rbCount; i++)
				{
					var rb = rigidBodies[i];
					if (!rb.HasCollider || rb.Entity == ent || (RelativeTeamFromEntity.Exists(rb.Entity) && RelativeTeamFromEntity[rb.Entity].Target == team))
						continue;

					var cc         = new CustomCollide(rb);
					var collection = new CustomCollideCollection(ref cc);
					if (collection.CastCollider(input, out var closestHit) && closestHit.Fraction < minFriction)
					{
						minFriction       = closestHit.Fraction;
						selectedRigidBody = i;

						hit = closestHit;
					}
				}

				return selectedRigidBody;
			}

			public void Execute(Entity ent, int index, ref RocketProjectile rocket, ref Translation translation, ref Velocity velocity, ref Relative<TeamDescription> team)
			{
				if (AgeTimeFromEntity.Exists(ent) && AgeTimeFromEntity[ent].EndMs < Tick.Ms)
				{
					Ecb.AddComponent(index, ent, EndedComponentType);
					Ecb.AddComponent(index, ent, OutOfTimeComponentType);
					return;
				}

				var     blobCollider = SphereCollider.Create(new SphereGeometry {Radius = rocket.DetectionRadius}, CollisionFilter.Default);
				ref var collider     = ref blobCollider.Value;

				var input = new ColliderCastInput
				{
					Collider = (Collider*) UnsafeUtility.AddressOf(ref collider)
				};
				var end = ProjectileUtility.Project(translation.Value, ref velocity.Value, Tick.Delta);

				input.Start = translation.Value;
				input.End   = end;

				if (Cast(ent, team.Target, input, out var hit) != -1)
				{
					Ecb.AddComponent(index, ent, EndedComponentType);
					Ecb.AddComponent(index, ent, new ProjectileExplodedEndReason {normal = hit.SurfaceNormal});

					end = hit.Position;
				}

				translation.Value = end;

				blobCollider.Dispose();
			}
		}

		private LazySystem<BuildPhysicsWorld>                                      m_BuildPhysicsWorld;
		private LazySystem<OrderGroup.Simulation.BeforeSpawnEntitiesCommandBuffer> m_Barrier;

		private EntityQuery m_Query;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_Query = GetEntityQuery(new EntityQueryDesc
			{
				All  = new ComponentType[] {typeof(RocketProjectile), typeof(Translation), typeof(Velocity), typeof(Relative<TeamDescription>)},
				None = new ComponentType[] {typeof(ProjectileEndedTag)}
			});
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			inputDeps = new UpdateJob
			{
				Tick         = GetTick(true),
				PhysicsWorld = this.L(ref m_BuildPhysicsWorld).PhysicsWorld,
				Ecb          = this.L(ref m_Barrier).CreateCommandBuffer().ToConcurrent(),

				EndedComponentType     = typeof(ProjectileEndedTag),
				OutOfTimeComponentType = typeof(ProjectileOutOfTimeEndReason),

				RelativeTeamFromEntity = GetComponentDataFromEntity<Relative<TeamDescription>>(true),
				AgeTimeFromEntity      = GetComponentDataFromEntity<ProjectileAgeTime>(true)
			}.Schedule(m_Query, JobHandle.CombineDependencies(m_BuildPhysicsWorld.Value.FinalJobHandle, inputDeps));
			m_Barrier.Value.AddJobHandleForProducer(inputDeps);

			return inputDeps;
		}
	}
}