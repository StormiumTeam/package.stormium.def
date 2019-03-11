using package.StormiumTeam.GameBase;
using StormiumTeam.GameBase;
using Stormium.Core;
using Stormium.Default.Kits.ProKit;
using StormiumShared.Core.Networking;
using Unity.Entities;
using Unity.Mathematics;

namespace Scripts.Actions.ProRailgun
{
	public struct ProRailgunAction : IStateData, IComponentData
	{
		public float ScanRadius;
		public int Damage;
	}
	
	[UpdateInGroup(typeof(ProActionSystemGroup))]
	public class ProRailgunActionSystem : ActionBaseSystem<ProRailgunActionSystem.SpawnRequest>
	{
		public struct SpawnRequest
		{
			public Entity Action;

			public float ScanRadius;

			public float3 Position;
			public float3 Direction;
		}

		protected override void OnActionUpdate()
		{
			ForEach((Entity entity, ref ProRailgunAction action, ref StActionSlotInput input, ref ActionAmmo ammo, ref ActionCooldown cooldown, ref OwnerState<LivableDescription> owner, ref EntityAuthority authority) =>
			{
				if (input.IsActive && cooldown.CooldownFinished(Tick) && ammo.Value >= ammo.Usage)
				{
					cooldown.StartTick = Tick;
					ammo.ModifyAmmo(ammo.Value - ammo.Usage);
					
					GetPositionAndDirection(owner.Target, out var position, out var view);

					SpawnRequests.Add(new SpawnRequest
					{
						Action = entity,
						ScanRadius = action.ScanRadius,
						
						Position  = position,
						Direction = view
					});
				}

				ammo.IncreaseFromDelta(TickDelta);
			});
		}

		protected override void FinalizeSpawnRequests()
		{
			var projProvider = World.GetExistingManager<ProRailgunProjectileProvider>();
			foreach (var request in SpawnRequests)
			{
				var projectile = projProvider.SpawnLocal(request.Position, request.Direction, new ProRailgunProjectile{ScanRadius = request.ScanRadius});
				
				EntityManager.ReplaceOwnerData(projectile, request.Action);
			}
		}
	}
}