using System;
using Runtime.BaseSystems;
using Stormium.Core;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Ray = Unity.Physics.Ray;

namespace Scripts.Gamemodes.TestMode
{
	public unsafe class TestMode : GameModeSystem
	{
		private EntityQuery m_BumpedEntityQuery;
		
		protected override void OnCreate()
		{
			base.OnCreate();

			m_BumpedEntityQuery = GetEntityQuery(new EntityQueryDesc
			{
				All = new ComponentType[] {typeof(LivableDescription), typeof(LocalToWorld), typeof(PhysicsCollider)},
				Any = new ComponentType[] {typeof(Velocity), typeof(PhysicsVelocity)}
			});
		}

		protected override void OnUpdate()
		{
			Entities.ForEach((Entity ee, ref ZoneEvent zoneEvent, ref ZoneRayRadius rayRadius, ref BumpZoneEvent bumpEvent) =>
			{
				var bumpForce = bumpEvent.Force;
				var radius    = rayRadius.Value;
				var input = new RaycastInput
				{
					Filter = CollisionFilter.Default,
					Ray    = new Ray(zoneEvent.Position, new float3(1))
				};

				Entities.With(m_BumpedEntityQuery).ForEach((ref LocalToWorld ltw, ref PhysicsCollider collider, ref PhysicsVelocity physicsVelocity, ref Velocity velocity) =>
				{
					input.Ray.Direction = new float3(math.normalize(ltw.Position - input.Ray.Origin) * radius);

					var collection = new CustomCollideCollection(new CustomCollide(collider, ltw));
					if (!collection.CastRay(input, out var closestHit))
						return;
					
					var pv = UnsafeUtility.AddressOf(ref physicsVelocity);
					var lv = UnsafeUtility.AddressOf(ref velocity);
					
					Debug.Log((IntPtr)lv);
					
					if (pv != null)
					{
						physicsVelocity.Linear -= closestHit.SurfaceNormal * bumpForce;
					}

					if (lv != null)
					{
						velocity.Value -= closestHit.SurfaceNormal * bumpForce;
					}
				});
			});
			
			var ecb = new EntityCommandBuffer(Allocator.TempJob);
			Entities.ForEach((Entity ee, ref ZoneEvent zoneEvent, ref ZoneRayRadius rayRadius, ref DamageZoneEvent dmgEvent) =>
			{
				var dmg = dmgEvent.Value;
				var radius    = rayRadius.Value;
				var input = new RaycastInput
				{
					Filter = CollisionFilter.Default,
					Ray    = new Ray(zoneEvent.Position, new float3(1))
				};

				Entities.WithAll<LivableDescription>().ForEach((Entity victim, ref LocalToWorld ltw, ref PhysicsCollider collider, ref PhysicsVelocity velocity) =>
				{
					input.Ray.Direction = new float3(math.normalize(ltw.Position - input.Ray.Origin) * radius);

					var collection = new CustomCollideCollection(new CustomCollide(collider, ltw));
					if (!collection.CastRay(input, out var closestHit))
						return;

					Debug.Log("DO some damage");
					
					var targetEvent = ecb.CreateEntity();

					ecb.AddComponent(targetEvent, new GameEvent());
					ecb.AddComponent(targetEvent, new TargetDamageEvent
					{
						DmgValue = -dmg,
						Shooter  = ee,
						Victim   = victim
					});
				});
			});	
			
			ecb.Playback(EntityManager);
			ecb.Dispose();
			
			World.GetExistingSystem<GameEventRuleSystemGroup>().Process();
		}
	}
}