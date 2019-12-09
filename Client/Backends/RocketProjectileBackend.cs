using System;
using Projectiles;
using Revolution;
using Unity.NetCode;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.BaseSystems;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Stormium.Default.Client.Visual
{
	public class RocketProjectileBackend : RuntimeAssetBackend<RocketProjectilePresentationBase>
	{
	}

	public abstract class RocketProjectilePresentationBase : RuntimeAssetPresentation<RocketProjectilePresentationBase>
	{
		public abstract bool CanBePooled { get; }
	}

	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	public class RocketProjectileHybridLink : HybridEntityLinkBase<RocketProjectileBackend>
	{
		private Lazy<AssetPool<GameObject>> m_BackendPool = new Lazy<AssetPool<GameObject>>(() => new AssetPool<GameObject>(pool =>
		{
			var gameObject = new GameObject("RocketProjectile Pooled");
			gameObject.SetActive(false);
			gameObject.AddComponent<RocketProjectileBackend>();
			gameObject.AddComponent<GameObjectEntity>();
			return gameObject;
		}));

		public override EntityQuery GetQuery()
		{
			return GetEntityQuery(typeof(RocketProjectile));
		}

		public override void OnResult(NativeArray<Entity> backendWithoutEntity, NativeArray<Entity> entityWithoutBackend)
		{
			foreach (var backendEntity in backendWithoutEntity)
			{				
				var backend = EntityManager.GetComponentObject<RocketProjectileBackend>(backendEntity);
				if (backend.Presentation == null || backend.Presentation.CanBePooled)
					backend.Return(true, true);
			}

			foreach (var entity in entityWithoutBackend)
			{
				var gameObject = m_BackendPool.Value.Dequeue();
				var backend    = gameObject.GetComponent<RocketProjectileBackend>();

				backend.SetTarget(EntityManager, entity);
				gameObject.SetActive(true);
			}
		}
	}
	
	[UpdateInGroup(typeof(AfterSnapshotIsAppliedSystemGroup))]
	[UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
	public class RocketStopSnapshotDestroy : ComponentSystem
	{
		protected override void OnUpdate()
		{
			Entities.WithAll<RocketProjectile>().WithNone<ManualDestroy>().ForEach(ent =>
			{
				EntityManager.AddComponent<ManualDestroy>(ent);
			});
		}
	}
}