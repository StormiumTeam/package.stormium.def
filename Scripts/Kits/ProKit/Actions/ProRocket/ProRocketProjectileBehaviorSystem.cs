using Scripts.Actions;
using Scripts.Provider;
using Stormium.Default.Kits.ProKit;
using StormiumShared.Core;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Data;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;

namespace Stormium.Default
{
	[UpdateInGroup(typeof(ProProjectileSystemGroup))]
	public class ProRocketProjectileBehaviorSystem : GameBaseSystem
	{
		public const float RocketProjectileRadius = 0.1f;

		private ComponentGroup       m_RocketGroup;
		private PhysicQueryManager m_PhysicQueryManager;

		private ProProjectileExplosionEventProvider m_ExplosionEventProvider;

		protected override void OnCreateManager()
		{
			base.OnCreateManager();

			m_RocketGroup = GetComponentGroup
			(
				ComponentType.ReadWrite<ProRocketProjectile>(),
				ComponentType.ReadWrite<Velocity>()
			);

			m_PhysicQueryManager = World.GetOrCreateManager<PhysicQueryManager>();
			m_ExplosionEventProvider = World.GetOrCreateManager<ProProjectileExplosionEventProvider>();
		}

		protected override void OnUpdate()
		{
			ForEach((Entity entity, ref ProProjectileData projectileData, ref ProRocketProjectile rocket, ref Translation translation, ref Velocity velocity, ref EntityAuthority authority) =>
			{
				if (projectileData.Phase != StandardProjectilePhase.Active)
				{
					// We give a small delay so clients can receive the explode effect
					if (projectileData.ExplodeTick + 1000 > Tick)
						return;

					PostUpdateCommands.DestroyEntity(entity);
					return;
				}

				var deltaTime = GetSingleton<GameTimeComponent>().Value.DeltaTime;
				var targetPosition = translation.Value + velocity.Value * deltaTime;
				var ray            = new Ray(translation.Value, normalizesafe(velocity.Value));

				m_PhysicQueryManager.EnableCollisionFor(entity);

				if (Physics.SphereCast(ray, max(rocket.Radius, 0.1f), out var hitInfo, length(velocity.Value) * deltaTime, GameBaseConstants.CollisionMask))
				{
					targetPosition             = hitInfo.point;
					projectileData.ExplodeTick = Tick;
				}

				m_PhysicQueryManager.ReenableCollisions();

				Debug.DrawLine(translation.Value, targetPosition, Color.black, deltaTime * 5);

				translation.Value = targetPosition;
			});

			// Set phases
			ForEach((Entity entity, ref ProProjectileData projectileData, ref ProRocketProjectile rocket, ref Translation translation, ref EntityAuthority authority) =>
			{
				// Need to explode
				if (projectileData.ExplodeTick != default && projectileData.Phase == StandardProjectilePhase.Active)
				{
					projectileData.Phase = StandardProjectilePhase.Exploded;

					var projPos = translation.Value;
					ForEach((Entity oe, Transform transform) =>
					{
						var collider = transform.GetComponent<Collider>();
						if (!collider)
							return;
						var velocity = default(Velocity);
						if (EntityManager.HasComponent<Velocity>(oe))
							velocity = EntityManager.GetComponentData<Velocity>(oe);
						
						var center = (float3) collider.bounds.center;

						var receiveExplosion = distance(center, projPos) < 3.5f;
						var receiveDamage    = distance(center, projPos) < 3f;

						if (!receiveExplosion && !receiveDamage)
							return;

						var delayedEvent = m_ExplosionEventProvider.SpawnLocalEntityDelayed(PostUpdateCommands);
						if (receiveExplosion)
						{
							var yBump = 6f;
							if (velocity.Value.y <= 0f)
								yBump = 9.5f;

							PostUpdateCommands.AddComponent(delayedEvent, new TargetBumpEvent
							{
								Position  = projPos,
								VelocityReset = float3(1, 1, 1),
								Direction = normalizesafe(center - projPos),
								Force     = new float3(12, yBump, 12),

								Shooter = entity,
								Victim  = oe
							});
						}

						if (receiveDamage)
						{
							const int dmg = 25;

							PostUpdateCommands.AddComponent(delayedEvent, new TargetDamageEvent
							{
								DmgValue = dmg,
								Shooter  = entity,
								Victim   = oe
							});
						}
					});
				}
			});
		}
	}
}