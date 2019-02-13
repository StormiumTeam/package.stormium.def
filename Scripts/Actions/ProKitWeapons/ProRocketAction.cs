using System.Collections.Generic;
using package.stormium.core;
using Runtime.Systems;
using Stormium.Core;
using Stormium.Default.States;
using StormiumShared.Core.Networking;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Stormium.Default
{
	public struct ProRocketAction : IStateData, IComponentData
	{
		public class Streamer : SnapshotEntityDataAutomaticStreamer<ProRocketAction>
		{
		}

		public ProRocketSettings RocketSettings;
	}

	public class ProRocketActionUpdateSystem : ActionBaseSystem<ProRocketActionUpdateSystem.SpawnRequest>
	{
		public const float ProjSpeed = 39f;

		public struct SpawnRequest
		{
			public Entity Action;
			public Entity Livable;

			public float3            Position;
			public float3            Velocity;
			public ProRocketSettings Settings;
		}

		protected override void OnActionUpdate()
		{
			ForEach((Entity                    e,
			         ref ProRocketAction       action,
			         ref StActionAmmo          ammo,
			         ref StActionAmmoCooldown  cooldown,
			         ref StActionInputFromSlot inputFromSlot,
			         ref StActionOwner         owner,
			         ref EntityAuthority       authority) =>
			{
				if (inputFromSlot.IsActive && cooldown.CooldownFinished(Tick) && ammo.Value > ammo.Usage)
				{
					// Restart cooldown...
					cooldown.StartTick =  Tick;
					ammo.Value         -= ammo.Usage;

					var aim = EntityManager.GetComponentData<AimLookState>(owner.LivableTarget);
					var pos = EntityManager.GetComponentData<TransformState>(owner.LivableTarget).Position + new float3(0, 1.6f, 0);
					var fwd = Quaternion.Euler(-aim.Aim.y, aim.Aim.x, 0) * Vector3.forward;

					SpawnRequests.Add(new SpawnRequest {Action = e, Position = pos, Velocity = fwd * ProjSpeed});
				}

				ammo.IncreaseFromDelta(TickDelta);
			});
		}

		protected override void FinalizeSpawnRequests()
		{
			var projProvider = World.GetExistingManager<ProRocketProjectileProvider>();
			foreach (var request in SpawnRequests)
			{
				var re = projProvider.SpawnLocal(request.Position, request.Velocity, request.Settings);

				EntityManager.AddComponentData(re, new OwnerToActionState {Target  = request.Action});
				EntityManager.AddComponentData(re, new OwnerToLivableState {Target = request.Livable});
			}
		}
	}
}