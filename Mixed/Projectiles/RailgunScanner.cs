using System;
using package.stormiumteam.shared.ecs;
using Revolution;
using Stormium.Core;
using Stormium.Core.Data;
using Stormium.Default.Mixed;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using Collider = Unity.Physics.Collider;
using SphereCollider = Unity.Physics.SphereCollider;

namespace Projectiles
{
	// not a projectile
	public struct RailgunScanner : IComponentData
	{
		public float Radius; // 0.01f
		public float MaxDistance;

		public struct Create
		{
			public RailgunScanner          Scanner;
			public ScannerDefaultExplosion OnExplode;

			public float3 Position;
			public float3 Direction;
			public Entity Owner;
		}

		[UpdateInGroup(typeof(OrderGroup.Simulation.SpawnEntities))]
		public class Provider : BaseProviderBatch<Create>
		{
			public override void GetComponents(out ComponentType[] entityComponents)
			{
				entityComponents = new ComponentType[]
				{
					typeof(ProjectileDescription),
					typeof(RailgunScanner),
					typeof(ScannerDefaultExplosion),
					typeof(Translation),
					typeof(Velocity),
					typeof(LocalToWorld),
					typeof(GhostEntity)
				};
			}

			public override void SetEntityData(Entity entity, Create data)
			{
				EntityManager.ReplaceOwnerData(entity, data.Owner);
				EntityManager.SetComponentData(entity, data.Scanner);
				EntityManager.SetComponentData(entity, data.OnExplode);
				EntityManager.SetComponentData(entity, new Translation {Value = data.Position});
				EntityManager.SetComponentData(entity, new Velocity {Value    = data.Direction});
			}
		}

		[UpdateInGroup(typeof(OrderGroup.Simulation.UpdateEntities.Interaction))]
		[UpdateAfter(typeof(ProRailgunWeaponComponent))]
		public unsafe class Process : JobGameBaseSystem
		{
			[BurstCompile]
			private struct Job : IJobForEachWithEntity<RailgunScanner, ScannerDefaultExplosion, Translation, Velocity, Relative<MovableDescription>>
			{
				private struct Hit : IEquatable<Entity>
				{
					public Entity entity;
					public float  distance;
					public float3 position;

					public bool Equals(Entity other)
					{
						return entity == other;
					}
				}

				public uint ServerTick;

				[ReadOnly]
				public PhysicsWorld PhysicsWorld;

				public EntityCommandBuffer.Concurrent DestroyEcb;
				public EntityCommandBuffer.Concurrent SpawnEcb;

				[ReadOnly] public BufferFromEntity<TransformHistory> TransformHistoryFromEntity;

				[ReadOnly] public ComponentDataFromEntity<LivableDescription>           LivableDescFromEntity;
				[ReadOnly] public ComponentDataFromEntity<Relative<LivableDescription>> RelativeLivableFromEntity;
				[ReadOnly] public ComponentDataFromEntity<Relative<PlayerDescription>>  RelativePlayerFromEntity;
				[ReadOnly] public ComponentDataFromEntity<GamePlayerUserCommand>        UserCommandFromEntity;

				[ReadOnly] public BufferFromEntity<DistanceDamageFallOf>  DamageFallOfFromEntity;
				[ReadOnly] public BufferFromEntity<DistanceImpulseFallOf> ImpulseFallOfFromEntity;

				public EntityArchetype ImpulseEventArchetype;
				public EntityArchetype DamageEventArchetype;

				bool Cast(Entity movable, in ColliderCastInput input, NativeList<Hit> hits, uint targetTick)
				{
					var targetHistoryIndex = (int) math.abs(ServerTick - targetTick);
					
					var rigidBodies = PhysicsWorld.Bodies;
					var rbCount     = rigidBodies.Length;
					var minFraction = float.MaxValue;
					var lastEntity  = default(Entity);
					var distance    = 0.0f;
					var hasHitWall  = false;
					var position    = default(float3);
					for (var i = 0; i < rbCount; i++)
					{
						var rb = rigidBodies[i];
						if (!rb.HasCollider || rb.Entity == movable)
							continue;

						var cc         = new CustomCollide(rb);
						var collection = new CustomCollideCollection(ref cc);
						if (collection.CastCollider(input, out var closestHit) && minFraction > closestHit.Fraction)
						{
							minFraction = closestHit.Fraction;
							if (LivableDescFromEntity.Exists(rb.Entity) && !hits.Contains(rb.Entity))
							{
								distance   = closestHit.Fraction;
								position   = closestHit.Position;
								lastEntity = rb.Entity;
								hasHitWall = false;
							}
							else
							{
								lastEntity = default;
								hasHitWall = true;
							}
						}
						else if (TransformHistoryFromEntity.Exists(rb.Entity) && targetHistoryIndex > 0 && !hits.Contains(rb.Entity))
						{
							var history = TransformHistoryFromEntity[rb.Entity];
							if (history.Length > 0)
							{

								var elem = history[math.min(targetHistoryIndex, history.Length - 1)];
								cc.WorldFromMotion.pos = elem.Position;
								cc.WorldFromMotion.rot = elem.Rotation;
								if (collection.CastCollider(input, out closestHit) && minFraction > closestHit.Fraction)
								{
									minFraction = closestHit.Fraction;
									distance    = closestHit.Fraction;
									position    = closestHit.Position;
									lastEntity  = rb.Entity;
									hasHitWall  = false;
								}
							}
						}
					}

					if (lastEntity.Version > 0)
						hits.Add(new Hit {entity = lastEntity, distance = 1 - distance, position = position});

					// if we did hit a wall or we didn't hit any entity, return false, else return true
					return !hasHitWall && lastEntity.Version > 0;
				}

				private void UpdateRadius(void* collider, float radius)
				{
					var sphereCollider = (SphereCollider*) collider;
					var geom           = sphereCollider->Geometry;
					geom.Radius              = math.max(0.001f, radius);
					sphereCollider->Geometry = geom;
				}

				private void DoCast(ref ColliderCastInput input, in RailgunScanner scanner, in Entity exclude, NativeList<Hit> hits, uint entityTick)
				{
					UpdateRadius(input.Collider, scanner.Radius * 1.0f);
					if (!Cast(exclude, input, hits, entityTick))
					{
						UpdateRadius(input.Collider, scanner.Radius * 0.1f);
						if (!Cast(exclude, input, hits, entityTick))
							return;
					}

					// if we can still hit without touching a wall, we continue
					DoCast(ref input, scanner, exclude, hits, entityTick);
				}

				public void Execute(Entity             ent,         int                         index,
				                    ref RailgunScanner scanner,     ref ScannerDefaultExplosion onExplode,
				                    ref Translation    translation, ref Velocity                velocity, ref Relative<MovableDescription> movable)
				{
					var     blobCollider = SphereCollider.Create(new SphereGeometry {Radius = scanner.Radius}, CollisionFilter.Default);
					ref var collider     = ref blobCollider.Value;

					var input = new ColliderCastInput
					{
						Collider = (Collider*) UnsafeUtility.AddressOf(ref collider)
					};
					var end = translation.Value + velocity.normalized * scanner.MaxDistance;

					input.Start = translation.Value;
					input.End   = end;

					// We do three cast, a *0.1 radius, *0.5 radius and the normal radius
					var hits       = new NativeList<Hit>(8, Allocator.Temp);
					var targetTick = 0u;
					if (RelativePlayerFromEntity.Exists(ent))
					{
						targetTick = UserCommandFromEntity[RelativePlayerFromEntity[ent].Target].Tick - 1;
					}

					DoCast(ref input, in scanner, movable.Target, hits, targetTick);

					for (var i = 0; i != hits.Length; i++)
					{
						float damage = onExplode.MaxDamage;
						if (DamageFallOfFromEntity.Exists(ent))
						{
							var buffer = DamageFallOfFromEntity[ent];
							var fallOf = buffer.GetFallOfResult<DistanceDamageFallOf, float>(hits[i].distance);
							damage = math.clamp(onExplode.MaxDamage * fallOf, onExplode.MinDamage, onExplode.MaxDamage);
						}

						float impulse = onExplode.ImpulseMax;
						if (ImpulseFallOfFromEntity.Exists(ent))
						{
							var buffer = ImpulseFallOfFromEntity[ent];
							var fallOf = buffer.GetFallOfResult<DistanceImpulseFallOf, float>(hits[i].distance);
							impulse = math.clamp(onExplode.ImpulseMax * fallOf, onExplode.ImpulseMin, onExplode.ImpulseMax);
						}

						Entity ev;
						ev = SpawnEcb.CreateEntity(index, DamageEventArchetype);
						SpawnEcb.SetComponent(index, ev, new TargetDamageEvent
						{
							Damage = Mathf.RoundToInt(-damage), Destination = hits[i].entity,
							Origin = RelativeLivableFromEntity.Exists(ent) ? RelativeLivableFromEntity[ent].Target : ent
						});
						SpawnEcb.AddComponent(index, ev, new Translation {Value = hits[i].position});

						ev = SpawnEcb.CreateEntity(index, ImpulseEventArchetype);
						SpawnEcb.SetComponent(index, ev, new TargetImpulseEvent
						{
							Force = velocity.normalized * impulse + new float3(0, 0.1f, 0), Momentum = 1, Destination = hits[i].entity, Origin = ent, Position = translation.Value
						});
					}

					blobCollider.Dispose();

					DestroyEcb.DestroyEntity(index, ent);
				}
			}

			private LazySystem<OrderGroup.Simulation.DeleteEntities.CommandBufferSystem> m_EndBuffer;
			private LazySystem<OrderGroup.Simulation.SpawnEntities.CommandBufferSystem>  m_StartBuffer;
			private LazySystem<BuildPhysicsWorld>                                        m_BuildPhysicsWorld;

			private EntityQuery m_Query;

			private EntityArchetype m_ImpulseEventArchetype;
			private EntityArchetype m_DamageEventArchetype;

			protected override void OnCreate()
			{
				base.OnCreate();

				m_Query = GetEntityQuery(typeof(ProjectileDescription), typeof(RailgunScanner), typeof(ScannerDefaultExplosion), typeof(Translation), typeof(Velocity), typeof(Relative<MovableDescription>));
			}

			protected override JobHandle OnUpdate(JobHandle inputDeps)
			{
				inputDeps = new Job
				{
					ServerTick = GetTick(true).AsUInt,

					PhysicsWorld              = m_BuildPhysicsWorld.Get(World).PhysicsWorld,
					DestroyEcb                = m_EndBuffer.Get(World).CreateCommandBuffer().ToConcurrent(),
					SpawnEcb                  = m_StartBuffer.Get(World).CreateCommandBuffer().ToConcurrent(),
					LivableDescFromEntity     = GetComponentDataFromEntity<LivableDescription>(true),
					RelativeLivableFromEntity = GetComponentDataFromEntity<Relative<LivableDescription>>(true),
					
					TransformHistoryFromEntity = GetBufferFromEntity<TransformHistory>(true),
					RelativePlayerFromEntity = GetComponentDataFromEntity<Relative<PlayerDescription>>(true),
					UserCommandFromEntity = GetComponentDataFromEntity<GamePlayerUserCommand>(true),

					ImpulseEventArchetype = World.GetExistingSystem<TargetImpulseEvent.Provider>().EntityArchetype,
					DamageEventArchetype  = World.GetExistingSystem<TargetDamageEvent.Provider>().EntityArchetype,

					DamageFallOfFromEntity  = GetBufferFromEntity<DistanceDamageFallOf>(true),
					ImpulseFallOfFromEntity = GetBufferFromEntity<DistanceImpulseFallOf>(true)
				}.Schedule(m_Query, inputDeps);
				m_EndBuffer.Value.AddJobHandleForProducer(inputDeps);
				m_StartBuffer.Value.AddJobHandleForProducer(inputDeps);
				return inputDeps;
			}
		}
	}
}