using System;
using System.Collections.Generic;
using package.stormiumteam.networking;
using package.stormiumteam.networking.runtime.lowlevel;
using package.StormiumTeam.GameBase;
using Scripts.Actions;
using Scripts.Actions.ProRailgun;
using Stormium.Core;
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

			public ComponentDataChangedFromEntity<ProProjectileData>    ProjectileDataFromEntity;
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
				ComponentType.ReadWrite<Velocity>(), 
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
				ModelId = GetModelIdent().Id,
				Runtime = snapshotRuntime,
				
				ProjectileDataFromEntity = new ComponentDataChangedFromEntity<ProProjectileData>(this),
				MinigunFromEntity        = new ComponentDataChangedFromEntity<ProMinigunProjectile>(this)
			}.Run();
		}

		public override void DeserializeCollection(ref DataBufferReader data, SnapshotSender sender, SnapshotRuntime snapshotRuntime)
		{

		}

		protected override Entity SpawnEntity(Entity origin, SnapshotRuntime snapshotRuntime)
		{
			return EntityManager.CreateEntity(EntityArchetype);
		}

		protected override void DestroyEntity(Entity worldEntity)
		{
			EntityManager.DestroyEntity(worldEntity);
		}

		public Entity SpawnLocal(float3 position, float3 velocity, Entity owner)
		{
			var entity = SpawnLocal();

			EntityManager.SetComponentData(entity, new Translation{Value = position});
			EntityManager.SetComponentData(entity, new Velocity(velocity));
			EntityManager.ReplaceOwnerData(entity, owner);
			EntityManager.SetComponentData(entity, new ProProjectileData{Phase = StandardProjectilePhase.Active});

			return entity;
		}
	}

	public class ProMinigunActionProvider : SystemProvider
	{
		protected override Entity SpawnEntity(Entity origin, SnapshotRuntime snapshotRuntime)
		{
			var entity = EntityManager.CreateEntity
			(
				ComponentType.ReadWrite<ModelIdent>(),
				ComponentType.ReadWrite<ActionDescription>(),
				ComponentType.ReadWrite<ActionTag>(),
				ComponentType.ReadWrite<ProMinigunAction.PredictedState>(),
				ComponentType.ReadWrite<ProMinigunAction.Settings>(),
				ComponentType.ReadWrite<StActionSlotInput>(),
				ComponentType.ReadWrite<ActionAmmo>(),
				ComponentType.ReadWrite<ActionSlot>(),
				ComponentType.ReadWrite<ActionCooldown>(),
				ComponentType.ReadWrite<GenerateEntitySnapshot>()
			);

			EntityManager.SetComponentData(entity, new ActionTag(TypeManager.GetTypeIndex(typeof(ProMinigunAction.Settings))));

			return entity;
		}

		protected override void DestroyEntity(Entity worldEntity)
		{
			EntityManager.DestroyEntity(worldEntity);
		}

		public Entity SpawnLocal(Entity owner, ProMinigunAction.Settings actionData, int slot)
		{
			var action = SpawnLocal();

			EntityManager.ReplaceOwnerData(action, owner);

			EntityManager.SetComponentData(action, new ActionSlot(slot));
			EntityManager.SetComponentData(action, new ActionAmmo(250, 6000));
			EntityManager.SetComponentData(action, new ActionCooldown(0, 100));
			EntityManager.SetComponentData(action, actionData);

			return action;
		}
	}
}