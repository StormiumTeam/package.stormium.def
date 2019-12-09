using System;
using Revolution;
using Unity.NetCode;
using Stormium.Core.Projectiles;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.VFX;

namespace Stormium.Default.Client.Visual
{
	public class DefaultRocketProjectilePresentation : RocketProjectilePresentationBase
	{
		public float timeBeforePooling = 3f;
		public VisualEffect vfx;
		
		[NonSerialized]
		public float PoolingProgress = 0.0f;

		[NonSerialized]
		public bool exploded;

		public override void OnReset()
		{
			base.OnReset();

			PoolingProgress = 0.0f;
			exploded = false;
			
			vfx.Reinit();
		}

		public override bool CanBePooled => PoolingProgress > timeBeforePooling;
	}

	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	[UpdateAfter(typeof(RocketProjectileHybridLink))]
	[UpdateAfter(typeof(SnapshotReceiveSystem))]
	public class SetRocketPresentation : GameBaseSystem
	{
		private AsyncAssetPool<GameObject>       m_Pool;
		private AsyncAssetPool<GameObject>       m_ExplosionPool;
		private ProjectileExplosionBackendSystem m_ExplosionBackendSystem;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_Pool          = new AsyncAssetPool<GameObject>("def/visuals/Projectile/Rocket.prefab");
			m_ExplosionPool = new AsyncAssetPool<GameObject>("def/visuals/Projectile/RocketExplosion.prefab");
			m_ExplosionPool.AddElements(8);

			m_ExplosionBackendSystem = World.GetOrCreateSystem<ProjectileExplosionBackendSystem>();
		}

		protected override void OnUpdate()
		{
			Entities.ForEach((RocketProjectileBackend backend) =>
			{
				if (backend.Presentation is DefaultRocketProjectilePresentation presentation)
				{
					Execute(backend, presentation);
					return;
				}

				backend.SetPresentationFromPool(m_Pool);
			});
		}

		private void Execute(RocketProjectileBackend backend, DefaultRocketProjectilePresentation presentation)
		{
			var ent = backend.DstEntity;
			if (!EntityManager.Exists(ent))
			{
				presentation.PoolingProgress += Time.DeltaTime;

				return;
			}

			presentation.PoolingProgress = 0.0f;
			if (EntityManager.HasComponent<ProjectileEndedTag>(ent))
			{
				presentation.vfx.Stop();
			}
			if (!presentation.exploded && EntityManager.HasComponent<ProjectileExplodedEndReason>(ent))
			{
				var explosionData = EntityManager.GetComponentData<ProjectileExplodedEndReason>(ent);
				var explosionBackend = m_ExplosionBackendSystem.Pool.Value
				                                               .Dequeue()
				                                               .GetComponent<ProjectileExplosionBackend>();
				if (explosionBackend.Presentation != null)
					explosionBackend.ReturnPresentationToPool();
				
				explosionBackend.SetTarget(EntityManager, ent);
				explosionBackend.SetPresentationFromPool(m_ExplosionPool);
				explosionBackend.transform.position = EntityManager.GetComponentData<Translation>(ent).Value + explosionData.normal * 0.1f;
				explosionBackend.transform.up = explosionData.normal;

				explosionBackend.gameObject.name = $"{World.Name} - Rocket Projectile Explosion";
				
				presentation.exploded = true;
			}
			
			presentation.transform.localPosition = EntityManager.GetComponentData<Translation>(ent).Value;
			presentation.transform.localRotation = Quaternion.LookRotation(EntityManager.GetComponentData<Velocity>(ent).Value);
			
			// OK, we finished with this entity if it was destroyed on the snapshot
			if (EntityManager.HasComponent<IsDestroyedOnSnapshot>(ent))
			{
				EntityManager.DestroyEntity(ent);
			}
		}
	}
}