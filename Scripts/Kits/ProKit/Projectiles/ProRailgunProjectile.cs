using package.StormiumTeam.GameBase;
using StormiumTeam.GameBase;
using Stormium.Default.Kits.ProKit;
using Unity.Entities;
using Unity.Mathematics;

namespace Scripts.Actions.ProRailgun
{
	public struct ProRailgunProjectile : IProjectile, IComponentData
	{
		public float  ScanRadius;
		public float3 HitPoint;
		public float3 Direction;
	}

	[DisableAutoCreation]
	public class ProRailgunProjectileSystem : GameBaseSystem
	{
		private PhysicQueryManager                m_PhysicQueryManager;
		private ProProjectileExplosionEventProvider m_ExplosionEventProvider;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_PhysicQueryManager     = World.GetOrCreateSystem<PhysicQueryManager>();
			m_ExplosionEventProvider = World.GetOrCreateSystem<ProProjectileExplosionEventProvider>();
		}

		protected override void OnUpdate()
		{
			/*ForEach((Entity entity, ref ProProjectileData projectileData, ref ProRailgunProjectile railgun, ref Translation translation, ref EntityAuthority authority) =>
			{
				if (projectileData.phase != StandardProjectilePhase.Active)
				{
					if (projectileData.explodeTick + 1000 > Tick)
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
					projectileData.explodeTick = Tick;
					projectileData.phase = StandardProjectilePhase.Exploded;

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

				projectileData.phase = StandardProjectilePhase.None;

				m_PhysicQueryManager.ReenableCollisions();
			});*/
		}
	}
}