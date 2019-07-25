using StormiumTeam.GameBase;
using package.stormiumteam.networking.runtime.lowlevel;
using package.StormiumTeam.GameBase;
using Stormium.Core;
using Stormium.Default.Kits.ProKit;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Scripts.Actions.ProRailgun
{
	public class ProRailgunProjectileProvider : SystemProvider
	{
		public override void GetComponents(out ComponentType[] entityComponents, out ComponentType[] excludedComponents)
		{
			entityComponents = new[]
			{
				ComponentType.ReadWrite<ProjectileDescription>(),
				ComponentType.ReadWrite<ProProjectile.Settings>(),
				ComponentType.ReadWrite<ProProjectile.PredictedState>(),
				ComponentType.ReadWrite<ProRailgunProjectile>(),
				ComponentType.ReadWrite<Translation>(),
				ComponentType.ReadWrite<Rotation>(),
				ComponentType.ReadWrite<LocalToWorld>(),
				ComponentType.ReadWrite<TransformState>(),
				ComponentType.ReadWrite<TransformStateDirection>(),
			};
			excludedComponents = null;
		}

		public override void SerializeCollection(ref DataBufferWriter data, SnapshotReceiver receiver, SnapshotRuntime snapshotRuntime)
		{
			foreach (var entity in snapshotRuntime.Entities)
			{
				if (entity.ModelId != GetModelIdent().Id) continue;

				var projectile = EntityManager.GetComponentData<ProRailgunProjectile>(entity.Source);
				var transform  = EntityManager.GetComponentData<TransformState>(entity.Source);

				data.WriteValue(projectile);
				data.WriteValue(transform.Position);
				data.WriteValue(transform.Rotation);
			}
		}

		public override void DeserializeCollection(ref DataBufferReader data, SnapshotSender sender, SnapshotRuntime snapshotRuntime)
		{
			foreach (var entity in snapshotRuntime.Entities)
			{
				if (entity.ModelId != GetModelIdent().Id) continue;

				var worldEntity = snapshotRuntime.EntityToWorld(entity.Source);

				EntityManager.SetComponentData(worldEntity, data.ReadValue<ProRailgunProjectile>());
				EntityManager.SetComponentData(worldEntity, new TransformState(data.ReadValue<float3>(), data.ReadValue<quaternion>()));
			}
		}

		protected override Entity SpawnEntity(Entity origin, SnapshotRuntime snapshotRuntime)
		{
			var entity = EntityManager.CreateEntity(EntityComponents);

			var cf = snapshotRuntime.Header.Sender.Flags;
			EntityManager.SetComponentData(entity, new TransformStateDirection(cf == SnapshotFlags.Local ? Dir.ConvertToState : Dir.ConvertFromState));

			return entity;
		}

		protected override void DestroyEntity(Entity worldEntity)
		{
			EntityManager.DestroyEntity(worldEntity);
		}

		public Entity SpawnLocal(float3 position, float3 direction, ProRailgunProjectile settings)
		{
			var entity = SpawnLocal();

			settings.Direction = direction;

			EntityManager.SetComponentData(entity, new Translation {Value = position});
			EntityManager.SetComponentData(entity, new Rotation {Value    = quaternion.LookRotationSafe(direction, new float3(0, 1, 0))});
			EntityManager.SetComponentData(entity, settings);
			EntityManager.SetComponentData(entity, new ProProjectile.PredictedState {phase = StandardProjectilePhase.Active});

			return entity;
		}
	}

	public class ProRailgunActionProvider : SystemProvider
	{
		protected override Entity SpawnEntity(Entity origin, SnapshotRuntime snapshotRuntime)
		{
			var entity = EntityManager.CreateEntity
			(
				ComponentType.ReadWrite<ModelIdent>(),
				ComponentType.ReadWrite<ActionDescription>(),
				ComponentType.ReadWrite<ActionTag>(),
				ComponentType.ReadWrite<ProRailgunAction>(),
				ComponentType.ReadWrite<StActionSlotInput>(),
				ComponentType.ReadWrite<ActionAmmo>(),
				ComponentType.ReadWrite<ActionSlot>(),
				ComponentType.ReadWrite<ActionCooldown>(),
				ComponentType.ReadWrite<GenerateEntitySnapshot>()
			);

			EntityManager.SetComponentData(entity, new ActionTag(TypeManager.GetTypeIndex(typeof(ProRailgunAction))));

			return entity;
		}

		protected override void DestroyEntity(Entity worldEntity)
		{
			EntityManager.DestroyEntity(worldEntity);
		}

		public Entity SpawnLocal(Entity owner, ProRailgunAction actionData, int slot)
		{
			var action = SpawnLocal();
			
			EntityManager.ReplaceOwnerData(action, owner);

			EntityManager.SetComponentData(action, new ActionSlot(slot));
			EntityManager.SetComponentData(action, new ActionAmmo(1000, 2000));
			EntityManager.SetComponentData(action, new ActionCooldown(0, 500));
			EntityManager.SetComponentData(action, actionData);
			
			return action;
		}
	}
}