using package.StormiumTeam.GameBase;
using StormiumTeam.GameBase;
using Scripts.Provider;
using Stormium.Default.Kits.ProKit;
using StormiumTeam.GameBase.Data;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

using static Unity.Mathematics.math;

namespace Scripts.Actions.ProRailgun
{
	public struct ProRailgunProjectile : IProjectile, IComponentData
	{
		public float  ScanRadius;
		public float3 HitPoint;
		public float3 Direction;
	}

	[UpdateInGroup(typeof(ProProjectileSystemGroup))]
	public class ProRailgunProjectileSystem : GameBaseSystem
	{
		private PhysicQueryManager                m_PhysicQueryManager;
		private ProProjectileExplosionEventProvider m_ExplosionEventProvider;

		protected override void OnCreateManager()
		{
			base.OnCreateManager();

			m_PhysicQueryManager     = World.GetOrCreateManager<PhysicQueryManager>();
			m_ExplosionEventProvider = World.GetOrCreateManager<ProProjectileExplosionEventProvider>();
		}

		protected override void OnUpdate()
		{
			ForEach((Entity entity, ref ProProjectileData projectileData, ref ProRailgunProjectile railgun, ref Translation translation, ref EntityAuthority authority) =>
			{
				if (projectileData.Phase != StandardProjectilePhase.Active)
				{
					if (projectileData.ExplodeTick + 1000 > Tick)
						return;

					PostUpdateCommands.DestroyEntity(entity);
					return;
				}

				var ray = new Ray(translation.Value, railgun.Direction);

				Debug.DrawRay(ray.origin, ray.direction * 10, Color.black, 1f);
				Debug.DrawRay(ray.origin, ray.direction * 10, Color.red, 0.1f);
				
				m_PhysicQueryManager.EnableCollisionFor(entity);

				if (Physics.SphereCast(ray, railgun.ScanRadius, out var hitInfo, 128.0f, GameBaseConstants.CollisionMask))
				{
					railgun.HitPoint           = hitInfo.point;
					projectileData.ExplodeTick = Tick;
					projectileData.Phase = StandardProjectilePhase.Exploded;

					var hitGameObjectEntity = hitInfo.collider.GetComponent<GameObjectEntity>();
					if (hitGameObjectEntity && hitGameObjectEntity.EntityManager == EntityManager)
					{
						var delayedEvent = m_ExplosionEventProvider.SpawnLocalEntityDelayed(PostUpdateCommands);
						
						Debug.Log("Railgun Hit: " + hitGameObjectEntity.Entity);

						PostUpdateCommands.AddComponent(delayedEvent, new TargetBumpEvent
						{
							Direction = ray.direction,
							Force     = float3(1, 1, 1),
							VelocityReset = float3(1, 1, 1),

							Position = railgun.HitPoint,
							Shooter  = entity,
							Victim   = hitGameObjectEntity.Entity
						});
						PostUpdateCommands.AddComponent(delayedEvent, new TargetDamageEvent
						{
							DmgValue = 3,
							Shooter  = entity,
							Victim   = hitGameObjectEntity.Entity
						});
						
						Debug.DrawLine(ray.origin, hitInfo.point, Color.green, 0.1f);
					}
				}

				projectileData.Phase = StandardProjectilePhase.None;

				m_PhysicQueryManager.ReenableCollisions();
			});
		}
	}
}