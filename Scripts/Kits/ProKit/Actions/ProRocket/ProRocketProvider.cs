using StormiumTeam.GameBase;
using package.stormiumteam.networking.runtime.lowlevel;
using package.stormiumteam.shared;
using package.StormiumTeam.GameBase;
using Scripts.Actions;
using Scripts.Actions.ProKitWeapons;
using Stormium.Core;
using StormiumShared.Core.Networking;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Stormium.Default
{
	public class ProRocketProjectileProvider : SystemProvider
	{
		private ComponentTypes m_Components;
		
		protected override void OnCreateManager()
		{
			base.OnCreateManager();
			
			m_Components = new ComponentTypes(new[]
			{
				ComponentType.ReadWrite<ModelIdent>(),
				ComponentType.ReadWrite<ProjectileDescription>(), 
				ComponentType.ReadWrite<Translation>(),
				ComponentType.ReadWrite<Rotation>(), 
				ComponentType.ReadWrite<LocalToWorld>(), 
				ComponentType.ReadWrite<TransformState>(), 
				ComponentType.ReadWrite<TransformStateDirection>(), 
				ComponentType.ReadWrite<CopyTransformToGameObject>(), 
				ComponentType.ReadWrite<ProProjectileData>(), 
				ComponentType.ReadWrite<ProRocketProjectile>(),
				ComponentType.ReadWrite<Velocity>(),
				ComponentType.ReadWrite<GenerateEntitySnapshot>()
			});
		}

		protected override Entity SpawnEntity(Entity origin, SnapshotRuntime snapshotRuntime)
		{
			var gameObject = new GameObject("ToSet");
			var goe        = gameObject.AddComponent<GameObjectEntity>();

			EntityManager.AddComponents(goe.Entity, m_Components);

			var loadModelBehavior = gameObject.AddComponent<LoadModelFromStringBehaviour>();

			loadModelBehavior.SpawnRoot = gameObject.transform;
			loadModelBehavior.AssetId   = "Stormium.Default.Actions.ProKitWeapon.ProRocket.Projectile";
			loadModelBehavior.OnLoadSetSubModelFor(EntityManager, goe.Entity);

			gameObject.AddComponent<DestroyGameObjectOnEntityDestroyed>();
			gameObject.name = $"ProRocket Projectile(o={origin}, s={goe.Entity})";
			
			var cf = snapshotRuntime.Header.Sender.Flags;
			EntityManager.SetComponentData(goe.Entity, cf == SnapshotFlags.Local ? new TransformStateDirection(Dir.ConvertToState) : new TransformStateDirection(Dir.ConvertFromState));
			
			return goe.Entity;
		} 

		protected override void DestroyEntity(Entity worldEntity)
		{
			var gameObject = EntityManager.GetComponentObject<Transform>(worldEntity).gameObject;
            
			Object.Destroy(gameObject);
		}

		public Entity SpawnLocal(float3 position, float3 velocity, ProRocketProjectile projectile)
		{
			var entity = SpawnLocal();

			EntityManager.SetComponentData(entity, new Translation {Value = position});
			EntityManager.SetComponentData(entity, new Velocity(velocity));
			EntityManager.SetComponentData(entity, projectile);
			EntityManager.SetComponentData(entity, new ProProjectileData{Phase = StandardProjectilePhase.Active});

			return entity;
		}
	}

	public class ProRocketActionProvider : SystemProvider
	{
		struct WriteModelData
		{
			public ComponentDataFromEntity<ActionAmmo>                      AmmoFromEntity;
			public ComponentDataFromEntity<DataChanged<ActionAmmo>>         AmmoChangeFromEntity;
			public ComponentDataFromEntity<ActionSlot>                      SlotFromEntity;
			public ComponentDataFromEntity<DataChanged<ActionSlot>>         SlotChangeFromEntity;
			public ComponentDataFromEntity<ActionCooldown>              CooldownFromEntity;
			public ComponentDataFromEntity<DataChanged<ActionCooldown>> CooldownChangeFromEntity;

			public void Write(Entity entity, ref DataBufferWriter data)
			{
				var ammo     = AmmoFromEntity[entity];
				var slot     = SlotFromEntity[entity];
				var cooldown = CooldownFromEntity[entity];

				byte mask       = 0;
				var  maskMarker = data.WriteByte(mask);

				if (AmmoChangeFromEntity[entity].IsDirty == 1)
				{
					data.WriteDynamicIntWithMask((ulong) ammo.Usage, (ulong) ammo.Value, (ulong) ammo.Max);
					MainBit.SetBitAt(ref mask, 0, 1);
				}

				if (SlotChangeFromEntity[entity].IsDirty == 1)
				{
					data.WriteDynamicInt((ulong) slot.Value);
					MainBit.SetBitAt(ref mask, 1, 1);
				}

				if (CooldownChangeFromEntity[entity].IsDirty == 1)
				{
					data.WriteDynamicIntWithMask((ulong) cooldown.Cooldown, (ulong) cooldown.StartTick);
					MainBit.SetBitAt(ref mask, 2, 1);
				}

				data.WriteByte(mask, maskMarker);
			}
		}

		protected override Entity SpawnEntity(Entity origin, SnapshotRuntime snapshotRuntime)
		{
			var entity = EntityManager.CreateEntity
			(
				ComponentType.ReadWrite<ModelIdent>(),
				ComponentType.ReadWrite<ActionDescription>(),
				ComponentType.ReadWrite<ActionTag>(),
				ComponentType.ReadWrite<ProRocketAction>(),
				ComponentType.ReadWrite<StActionSlotInput>(),
				ComponentType.ReadWrite<ActionAmmo>(),
				ComponentType.ReadWrite<ActionSlot>(),
				ComponentType.ReadWrite<ActionCooldown>(),
				ComponentType.ReadWrite<GenerateEntitySnapshot>()
			);
			
			EntityManager.SetComponentData(entity, new ActionTag(TypeManager.GetTypeIndex(typeof(ProRocketAction))));

			return entity;
		}

		protected override void DestroyEntity(Entity worldEntity)
		{
			
		}

		public Entity SpawnLocal(Entity owner, int slot)
		{
			var action = SpawnLocal();
			
			/*EntityManager.AddComponentData(action, new OwnerSState<LivableDescription>{Target = livable});
			EntityManager.AddComponentData(action, new OwnerState<PlayerDescription>{Target = player});*/
			EntityManager.ReplaceOwnerData(action, owner);
			
			EntityManager.SetComponentData(action, new ActionSlot(slot));
			EntityManager.SetComponentData(action, new ActionCooldown(0, 300));
			EntityManager.SetComponentData(action ,new ActionAmmo(1500, 3000));
			EntityManager.SetComponentData(action, new ProRocketAction
			{
				RocketProjectile = new ProRocketProjectile
				{
					Radius = 0.1f
				}
			});

			return action;
		}
	}
	
	public class ProRocketDetonateActionProvider : SystemProvider
	{
		protected override Entity SpawnEntity(Entity origin, SnapshotRuntime snapshotRuntime)
		{
			var entity = EntityManager.CreateEntity
			(
				ComponentType.ReadWrite<ModelIdent>(),
				ComponentType.ReadWrite<ActionDescription>(),
				ComponentType.ReadWrite<ActionTag>(),
				ComponentType.ReadWrite<ProRocketDetonateAction>(),
				ComponentType.ReadWrite<StActionSlotInput>(),
				ComponentType.ReadWrite<ActionSlot>(),
				ComponentType.ReadWrite<ActionCooldown>(),
				ComponentType.ReadWrite<GenerateEntitySnapshot>()
			);
			
			EntityManager.SetComponentData(entity, new ActionTag(TypeManager.GetTypeIndex(typeof(ProRocketDetonateAction))));

			return entity;
		}

		protected override void DestroyEntity(Entity worldEntity)
		{
			
		}

		public Entity SpawnLocal(Entity livable, Entity player, int slot)
		{
			var action = SpawnLocal();

			EntityManager.AddComponentData(action, new OwnerState<LivableDescription> {Target = livable});
			EntityManager.AddComponentData(action, new OwnerState<PlayerDescription> {Target  = player});
			
			EntityManager.SetComponentData(action, new ActionSlot(slot));
			EntityManager.SetComponentData(action, new ActionCooldown(0, 500));

			return action;
		}
	}
}