using package.stormium.core;
using Runtime;
using Scripts.Actions.ProKitWeapons;
using StandardAssets.Characters.Physics;
using Stormium.Core;
using Stormium.Default.States;
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
				ComponentType.Create<ModelIdent>(),
				ComponentType.Create<Position>(),
				ComponentType.Create<Rotation>(), 
				ComponentType.Create<TransformState>(), 
				ComponentType.Create<TransformStateDirection>(), 
				ComponentType.Create<ProjectileTag>(),
				ComponentType.Create<ProRocketSettings>(),
				ComponentType.Create<Velocity>(),
				ComponentType.Create<GenerateEntitySnapshot>()
			});
		}

		public override Entity SpawnEntity(Entity origin, StSnapshotRuntime snapshotRuntime)
		{
			var gameObject = new GameObject("ToSet");
			var goe        = gameObject.AddComponent<GameObjectEntity>();

			EntityManager.AddComponents(goe.Entity, m_Components);
			EntityManager.AddComponent(goe.Entity, typeof(CopyTransformToGameObject));

			var loadModelBehavior = gameObject.AddComponent<LoadModelFromStringBehaviour>();

			loadModelBehavior.SpawnRoot = gameObject.transform;
			loadModelBehavior.AssetId   = "Stormium.Default.Actions.ProKitWeapon.ProRocket.Projectile";

			gameObject.AddComponent<DestroyGameObjectOnEntityDestroyed>();
			gameObject.name = $"ProRocket Projectile(o={origin}, s={goe.Entity})";
			
			var cf = snapshotRuntime.Header.Sender.Flags;
			EntityManager.SetComponentData(origin, cf == SnapshotFlags.Local ? new TransformStateDirection(Dir.ConvertToState) : new TransformStateDirection(Dir.ConvertFromState));

			return goe.Entity;
		}

		public override void DestroyEntity(Entity worldEntity)
		{
			var gameObject = EntityManager.GetComponentObject<Transform>(worldEntity).gameObject;
            
			Object.Destroy(gameObject);
		}

		public Entity SpawnLocal(float3 position, float3 velocity, ProRocketSettings settings)
		{
			var entity = SpawnLocal();

			EntityManager.SetComponentData(entity, new Position {Value = position});
			EntityManager.SetComponentData(entity, new Velocity(velocity));
			EntityManager.SetComponentData(entity, settings);

			return entity;
		}
	}

	public class ProRocketActionProvider : SystemProvider
	{
		public override Entity SpawnEntity(Entity origin, StSnapshotRuntime snapshotRuntime)
		{
			var entity = EntityManager.CreateEntity
			(
				ComponentType.Create<ModelIdent>(),
				ComponentType.Create<StActionTag>(),
				ComponentType.Create<ProRocketAction>(),
				ComponentType.Create<StActionInputFromSlot>(),
				ComponentType.Create<StActionOwner>(),
				ComponentType.Create<StActionAmmo>(),
				ComponentType.Create<StActionSlot>(),
				ComponentType.Create<StActionAmmoCooldown>()
			);
			
			EntityManager.SetComponentData(entity, new StActionTag(TypeManager.GetTypeIndex(typeof(ProRocketAction))));

			return entity;
		}

		public override void DestroyEntity(Entity worldEntity)
		{
			
		}

		public Entity SpawnLocal(Entity ownerLivable, Entity ownerInput, int slot)
		{
			var action = SpawnLocal();
			
			EntityManager.SetComponentData(action, new StActionOwner(ownerLivable, ownerInput));
			EntityManager.SetComponentData(action, new StActionAmmoCooldown(0, 300));
			EntityManager.SetComponentData(action ,new StActionAmmo(1250, 3000));
			EntityManager.SetComponentData(action, new ProRocketAction
			{
				RocketSettings = new ProRocketSettings
				{
					Radius = 0.1f
				}
			});

			return action;
		}
	}
}