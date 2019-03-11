using package.StormiumTeam.GameBase;
using StormiumTeam.GameBase;
using Stormium.Core;
using Stormium.Default.Kits.ProKit;
using StormiumShared.Core.Networking;
using StormiumTeam.GameBase.Data;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Stormium.Default
{
	public struct ProRocketAction : IStateData, IComponentData
	{
		public ProRocketProjectile RocketProjectile;
		public int LastShoot;
	}

	[UpdateInGroup(typeof(ProActionSystemGroup))]
	public class ProRocketActionUpdateSystem : ActionBaseSystem<ProRocketActionUpdateSystem.SpawnRequest>
	{
		public const float ProjSpeed = 36f;

		public struct SpawnRequest
		{
			public Entity Action;
			public Entity Livable;
			public Entity Player;

			public float3            Position;
			public float3            Velocity;
			public ProRocketProjectile Projectile;
		}

		private AudioClip m_FireAudio;
		private int m_LastShootTick;
		
		protected override void OnStartRunning()
		{
			Addressables.LoadAsset<AudioClip>("Stormium.Default.Actions.ProKitWeapon.ProRocket.FireSound").Completed += op => { m_FireAudio = op.Result; };
		}

		protected override void OnActionUpdate()
		{
			ForEach((Entity                             e,
			         ref ProRocketAction                action,
			         ref ActionAmmo                   ammo,
			         ref ActionCooldown           cooldown,
			         ref StActionSlotInput          inputFromSlot,
			         ref OwnerState<LivableDescription> livableOwner,
			         ref EntityAuthority                authority) =>
			{
				if (inputFromSlot.IsActive && cooldown.CooldownFinished(Tick) && ammo.Value >= ammo.Usage)
				{
					// Restart cooldown...
					cooldown.StartTick =  Tick;
					ammo.ModifyAmmo(ammo.Value - ammo.Usage);

					var aim = EntityManager.GetComponentData<AimLookState>(livableOwner.Target);
					var pos = EntityManager.GetComponentData<TransformState>(livableOwner.Target).Position + EntityManager.GetComponentData<EyePosition>(livableOwner.Target).Value;
					var fwd = Quaternion.Euler(-aim.Aim.y, aim.Aim.x, 0) * Vector3.forward;

					SpawnRequests.Add(new SpawnRequest
					{
						Action     = e,
						Livable    = livableOwner.Target,
						Position   = pos,
						Velocity   = fwd * ProjSpeed,
						Projectile = action.RocketProjectile
					});

					action.LastShoot = Tick;
				}

				ammo.IncreaseFromDelta(TickDelta);
			});

			ForEach((ref ProRocketAction action, ref OwnerState<LivableDescription> livableOwner) =>
			{
				//Debug.Log($"{action.LastShoot} {m_LastShootTick}");
				if (action.LastShoot <= m_LastShootTick)
					return;

				m_LastShootTick = action.LastShoot;

				var state = EntityManager.GetComponentData<TransformState>(livableOwner.Target);
				AudioSource.PlayClipAtPoint(m_FireAudio, state.Position, 1f);
			});
		}

		protected override void FinalizeSpawnRequests()
		{
			var projProvider = World.GetExistingManager<ProRocketProjectileProvider>();
			foreach (var request in SpawnRequests)
			{
				var re = projProvider.SpawnLocal(request.Position, request.Velocity, request.Projectile);
				
				EntityManager.ReplaceOwnerData(re, request.Action);
				EntityManager.AddComponentData(re, new DestroyChainReaction(request.Action));
			}
		}
	}
}