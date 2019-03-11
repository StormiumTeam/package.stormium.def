using System;
using System.Collections.Generic;
using package.stormiumteam.networking;
using package.stormiumteam.networking.runtime.lowlevel;
using Scripts.Actions;
using StormiumShared.Core.Networking;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Stormium.Default.Actions.ProMinigun
{
	public class ProMinigunProjectileProvider : SystemProvider
	{
		private struct SerializeCollectionJob : IJob
		{
			public int ModelId;

			public SnapshotRuntime Runtime;

			public ComponentDataChangedFromEntity<ProProjectileData> ProjectileDataFromEntity;
			public ComponentDataChangedFromEntity<ProMinigunProjectile> MinigunFromEntity;
			
			public void Execute()
			{
				for (var i = 0; i != Runtime.Entities.Length; i++)
				{
					if (Runtime.Entities[i].ModelId != ModelId) continue;

					var entity = Runtime.Entities[i].Source;
				}
			}
		}

		public override void GetComponents(out ComponentType[] entityComponents, out ComponentType[] excludedComponents)
		{
			entityComponents = new[]
			{
				ComponentType.ReadWrite<ProjectileDescription>(),
				ComponentType.ReadWrite<ProProjectileData>(),
				ComponentType.ReadWrite<ProMinigunProjectile>(),
				ComponentType.ReadWrite<Translation>(),
				ComponentType.ReadWrite<Rotation>(),
				ComponentType.ReadWrite<LocalToWorld>(), 
				ComponentType.ReadWrite<TransformState>(),
				ComponentType.ReadWrite<TransformStateDirection>(),
			};
			excludedComponents = null;
		}

		public override void ExcludeComponentsFor(Type type, List<ComponentType> components)
		{
			// we don't want others to serialize our projectile structure
			components.Add(ComponentType.ReadWrite<ProMinigunProjectile>());
		}

		public override void SerializeCollection(ref DataBufferWriter data, SnapshotReceiver receiver, SnapshotRuntime snapshotRuntime)
		{
			new SerializeCollectionJob
			{
				ProjectileDataFromEntity = new ComponentDataChangedFromEntity<ProProjectileData>(this),
				MinigunFromEntity        = new ComponentDataChangedFromEntity<ProMinigunProjectile>(this)
			}.Run();
		}

		public override void DeserializeCollection(ref DataBufferReader data, SnapshotSender sender, SnapshotRuntime snapshotRuntime)
		{
			
		}

		protected override Entity SpawnEntity(Entity origin, SnapshotRuntime snapshotRuntime)
		{
			return default;
		}

		protected override void DestroyEntity(Entity worldEntity)
		{
			
		}

		public Entity SpawnLocal(float3 position, float3 direction, Entity owner)
		{
			var entity = SpawnLocal();

			EntityManager.ReplaceOwnerData(entity, owner);
			
			return entity;
		}
	}
}