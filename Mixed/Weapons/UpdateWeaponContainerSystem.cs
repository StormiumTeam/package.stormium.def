using Unity.NetCode;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Stormium.Default.Mixed
{
	[UpdateInGroup(typeof(OrderGroup.Simulation.ConfigureSpawnedEntities))]
	public class UpdateWeaponContainerSystem : JobGameBaseSystem
	{
		[BurstCompile]
		private struct ClearBufferJob : IJobForEach_B<WeaponContainer>
		{
			public void Execute(DynamicBuffer<WeaponContainer> buffer)
			{
				buffer.Clear();
			}
		}

		[BurstCompile]
		private struct UpdateBufferJob : IJobForEachWithEntity<Owner>
		{
			public BufferFromEntity<WeaponContainer> Container;

			[BurstDiscard]
			private void NonBurst_ThrowException(Entity source, Entity owner)
			{
				if (owner == default)
					return;
				Debug.LogError($"No WeaponContainer found on owner={owner}, source={source}");
			}

			public void Execute(Entity entity, int i, ref Owner owner)
			{
				if (owner.Target == default || !Container.Exists(owner.Target))
				{
					NonBurst_ThrowException(entity, owner.Target);
					return;
				}

				Container[owner.Target].Add(new WeaponContainer(entity));
			}
		}


		private EntityQuery m_WeaponHolderQuery;
		private EntityQuery m_OwnerQuery;
		private EntityQuery m_DataQuery;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_OwnerQuery = GetEntityQuery(typeof(WeaponContainer));
			m_WeaponHolderQuery = GetEntityQuery(new EntityQueryDesc
			{
				All  = new ComponentType[] {typeof(WeaponHolderDescription)},
				None = new ComponentType[] {typeof(WeaponContainer)}
			});
			m_DataQuery = GetEntityQuery(typeof(Owner), typeof(WeaponDescription));
		}

		protected override JobHandle OnUpdate(JobHandle inputDeps)
		{
			if (m_WeaponHolderQuery.CalculateEntityCount() > 0)
			{
				var entities = m_WeaponHolderQuery.ToEntityArray(Allocator.TempJob);
				foreach (var ent in entities)
				{
					var buffer = EntityManager.AddBuffer<WeaponContainer>(ent);
					buffer.Reserve(buffer.Capacity + 1);
				}

				entities.Dispose();
			}

			inputDeps = new ClearBufferJob().Schedule(m_OwnerQuery, inputDeps);
			inputDeps = new UpdateBufferJob
			{
				Container = GetBufferFromEntity<WeaponContainer>()
			}.ScheduleSingle(m_DataQuery, inputDeps);

			return inputDeps;
		}
	}
}