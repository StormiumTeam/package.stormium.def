using Stormium.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;

namespace Stormium.Default
{
	public class ProRocketProjectileBehaviorSystem : ComponentSystem
	{
		public const float RocketProjectileRadius = 0.1f;

		private ComponentGroup    m_RocketGroup;
		private StGameTimeManager m_GameTimeManager;
		private StPhysicQueryManager m_PhysicQueryManager;

		protected override void OnCreateManager()
		{
			m_RocketGroup = GetComponentGroup
			(
				ComponentType.Create<ProRocketSettings>(),
				ComponentType.Create<Velocity>()
			);

			m_GameTimeManager = World.GetOrCreateManager<StGameTimeManager>();
			m_PhysicQueryManager = World.GetExistingManager<StPhysicQueryManager>();
		}

		protected override void OnUpdate()
		{
			ForEach((Entity entity, ref ProRocketSettings settings, ref Position position, ref Velocity velocity, ref EntityAuthority authority) =>
			{	
				var deltaTime = m_GameTimeManager.GetTimeFromSingleton().DeltaTime;
				var targetPosition = position.Value + velocity.Value * deltaTime;
				var ray = new Ray(position.Value, normalizesafe(velocity.Value));
				
				m_PhysicQueryManager.EnableCollisionFor(entity);
				
				if (Physics.SphereCast(ray, max(settings.Radius, 0.1f), out var hitInfo, length(velocity.Value) * deltaTime, Constants.CollisionMask))
				{
					Debug.Log("Explosionnn!");
					Debug.DrawRay(hitInfo.point, hitInfo.normal * 2.5f, Color.red, deltaTime * 5);
					
					PostUpdateCommands.DestroyEntity(entity);
				}
				
				m_PhysicQueryManager.ReenableCollisions();
				
				Debug.DrawLine(position.Value, targetPosition, Color.black, deltaTime * 5);

				position.Value = targetPosition;
			});
		}
	}
}