using System;
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

namespace Stormium.Default.Actions.ProMinigun
{
	[Serializable]
	public struct ProMinigunProjectile : IComponentData
	{
		public float radius;
	}
	
	[UpdateInGroup(typeof(ProProjectileSystemGroup))]
	public class ProMinigunProjectileSystem : GameBaseSystem
	{
		protected override void OnUpdate()
		{
			ForEach((Entity entity, ref ProProjectileData projectileData, ref ProMinigunProjectile minigun, ref Translation translation, ref Velocity velocity, ref EntityAuthority authority) =>
			{
				if (projectileData.Phase != StandardProjectilePhase.Active)
				{
					// We give a small delay so clients can receive the explode effect
					if (projectileData.ExplodeTick + 1000 > Tick)
						return;

					PostUpdateCommands.DestroyEntity(entity);
					return;
				}

				var deltaTime      = GetSingleton<SingletonGameTime>().DeltaTime;
				var targetPosition = translation.Value + velocity.Value * deltaTime;
				var ray            = new Ray(translation.Value, normalizesafe(velocity.Value));

				PhysicQueryManager.EnableCollisionFor(entity);

				if (Physics.SphereCast(ray, max(minigun.radius, 0.1f), out var hitInfo, length(velocity.Value) * deltaTime, GameBaseConstants.CollisionMask))
				{
					targetPosition             = hitInfo.point;
					projectileData.ExplodeTick = Tick;
				}

				PhysicQueryManager.ReenableCollisions();

				Debug.DrawLine(translation.Value, targetPosition, Color.black, deltaTime * 5);

				translation.Value = targetPosition;
			});

			var explosionEventProvider = World.GetExistingManager<ProProjectileExplosionEventProvider>();
			// Set phases
			ForEach((Entity entity, ref ProProjectileData projectileData, ref ProMinigunProjectile minigun, ref Translation translation, ref EntityAuthority authority) =>
			{
				// Need to explode
				if (projectileData.ExplodeTick != default && projectileData.Phase == StandardProjectilePhase.Active)
				{
					projectileData.Phase = StandardProjectilePhase.Exploded;

					var projPos = translation.Value;
					Entities.WithAll<LivableDescription>().ForEach((Entity oe, Transform transform) =>
					{
						Debug.Log(transform.name);
						var collider = transform.GetComponent<Collider>();
						if (!collider)
							return;

						var center = (float3) collider.bounds.center;

						var receiveExplosion = distance(center, projPos) < 1f;
						var receiveDamage    = distance(center, projPos) < 1f;

						if (!receiveExplosion && !receiveDamage)
							return;

						var delayedEvent = explosionEventProvider.SpawnLocalEntityDelayed(PostUpdateCommands);
						if (receiveExplosion)
						{
							var yBump = 0.25f;

							PostUpdateCommands.AddComponent(delayedEvent, new TargetBumpEvent
							{
								Position      = projPos,
								VelocityReset = float3(1, 1, 1),
								Direction     = normalizesafe(center - projPos),
								Force         = new float3(0.25f, yBump, 0.25f),

								Shooter = entity,
								Victim  = oe
							});
						}

						if (receiveDamage)
						{
							const int dmg = 3;

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