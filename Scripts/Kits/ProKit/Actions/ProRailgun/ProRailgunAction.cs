using package.StormiumTeam.GameBase;
using StormiumTeam.GameBase;
using Stormium.Core;
using Unity.Entities;
using Unity.Mathematics;

namespace Scripts.Actions.ProRailgun
{
	public struct 	ProRailgunAction : IStateData, IComponentData
	{
		public float ScanRadius;
		public int Damage;
	}
	
	[DisableAutoCreation]
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
			Entities.ForEach((Entity entity, ref ProRailgunAction action, ref StActionSlotInput input, ref ActionAmmo ammo, ref ActionCooldown cooldown, ref Owner owner, ref EntityAuthority authority) =>
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
			var projProvider = World.GetExistingSystem<ProRailgunProjectileProvider>();
			for (var i = 0; i != SpawnRequests.Length; i++)
			{
				var request = SpawnRequests[i];
				var projectile = projProvider.SpawnLocal(request.Position, request.Direction, new ProRailgunProjectile{ScanRadius = request.ScanRadius});
				
				EntityManager.ReplaceOwnerData(projectile, request.Action);
			}
		}
	}
}