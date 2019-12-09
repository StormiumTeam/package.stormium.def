using DefaultNamespace.Components.InGame;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace DefaultNamespace.Systems.InGame
{
	[UpdateInGroup(typeof(OrderGroup.Simulation.UpdateEntities.Interaction))]
	public class KillZoneSystem : JobComponentSystem
	{
		private EntityQuery m_LivableQuery;
		private EntityQuery m_ZoneQuery;

		private OrderGroup.Simulation.SpawnEntities.CommandBufferSystem m_SpawnBuffer;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_LivableQuery = GetEntityQuery(typeof(LivableDescription), typeof(LivableHealth), typeof(LocalToWorld), typeof(PhysicsCollider));
			m_ZoneQuery    = GetEntityQuery(typeof(KillZone), typeof(LocalToWorld), typeof(PhysicsCollider));

			m_SpawnBuffer = World.GetOrCreateSystem<OrderGroup.Simulation.SpawnEntities.CommandBufferSystem>();
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			inputDeps = new UpdateJob
			{
				Ecb                 = m_SpawnBuffer.CreateCommandBuffer().ToConcurrent(),
				LivableChunks       = m_LivableQuery.CreateArchetypeChunkArray(Allocator.TempJob),
				EntityType          = GetArchetypeChunkEntityType(),
				HealthType          = GetArchetypeChunkComponentType<LivableHealth>(true),
				PhysicsColliderType = GetArchetypeChunkComponentType<PhysicsCollider>(true),
				LocalToWorldType    = GetArchetypeChunkComponentType<LocalToWorld>(true)
			}.Schedule(m_ZoneQuery, inputDeps);
			m_SpawnBuffer.AddJobHandleForProducer(inputDeps);

			return inputDeps;
		}

		[BurstCompile]
		private unsafe struct UpdateJob : IJobForEachWithEntity<LocalToWorld, PhysicsCollider>
		{
			public EntityCommandBuffer.Concurrent Ecb;

			[DeallocateOnJobCompletion, ReadOnly]
			public NativeArray<ArchetypeChunk> LivableChunks;

			[ReadOnly] public ArchetypeChunkEntityType                     EntityType;
			[ReadOnly] public ArchetypeChunkComponentType<LivableHealth>   HealthType;
			[ReadOnly] public ArchetypeChunkComponentType<PhysicsCollider> PhysicsColliderType;
			[ReadOnly] public ArchetypeChunkComponentType<LocalToWorld>    LocalToWorldType;

			[NativeSetThreadIndex]
			private int m_ThreadIndex;

			public void Execute(Entity zoneEntity, int index, [ReadOnly] ref LocalToWorld ltw, [ReadOnly] ref PhysicsCollider physColl)
			{
				for (var c = 0; c != LivableChunks.Length; c++)
				{
					var chunk                = LivableChunks[c];
					var livableEntityArray   = chunk.GetNativeArray(EntityType);
					var livableHealthArray   = chunk.GetNativeArray(HealthType);
					var livableColliderArray = chunk.GetNativeArray(PhysicsColliderType);
					var livableLtwArray      = chunk.GetNativeArray(LocalToWorldType);
					for (var ent = 0; ent != chunk.Count; ent++)
					{
						if (livableHealthArray[ent].IsDead)
							continue;

						if (CheckCollision(ltw, physColl,
							livableLtwArray[ent], livableColliderArray[ent]))
						{
							var ev = Ecb.CreateEntity(index);
							Ecb.AddComponent(index, ev, new GameEvent());
							Ecb.AddComponent(index, ev, new TargetDamageEvent {Damage = int.MinValue, Destination = livableEntityArray[ent], Origin = zoneEntity});
						}
					}
				}
			}

			private bool CheckCollision(LocalToWorld zoneLtw,    PhysicsCollider zoneColl,
			                            LocalToWorld livableLtw, PhysicsCollider livableColl)
			{
				var zoneAabb    = zoneColl.ColliderPtr->CalculateAabb(new RigidTransform(zoneLtw.Value));
				var livableAabb = livableColl.ColliderPtr->CalculateAabb(new RigidTransform(livableLtw.Value));
				if (!zoneAabb.Overlaps(livableAabb))
					return false;

				var cc         = new CustomCollide(livableColl, livableLtw);
				var collection = new CustomCollideCollection(ref cc);
				var b = collection.CalculateDistance(new ColliderDistanceInput
				{
					Collider    = zoneColl.ColliderPtr,
					MaxDistance = 0.0f,
					Transform   = new RigidTransform(zoneLtw.Value)
				}, out var closest);
				return b;
			}
		}
	}
}