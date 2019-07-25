using package.StormiumTeam.GameBase;
using Scripts.ActionBase;
using Stormium.Core;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Stormium.Default.Kits.ProKit.ProPulseGun
{
	public struct ProPulseGunAction : IComponentData
	{
		public int StartDamage;
		public int DecreaseValue;
		public int MaxDecrease;

		public struct ShootEvent : IShootEvent, IComponentData
		{
			public Entity Target { get; set; }
		}

		public struct FillJob : DefaultActionBaseSystem<ProPulseGunAction, FillJob, ShootEvent>.IFillJob
		{
			[NativeDisableParallelForRestriction] public NativeList<(float3 p, float3 v, int d, Entity o)> CreateProjectileList;
			
			public void Setup()
			{
				CreateProjectileList = World.Active.GetExistingSystem<ProPulseGunProjectile.Provider>().GetEntityDelayedList();
			}

			public void Shoot(ShootEvent ev, ProPulseGunAction action, Entity owner, ActionBaseSystem.ShootHelper sh)
			{
			}
		}
		
		public class System : DefaultActionBaseSystem<ProPulseGunAction, FillJob, ShootEvent>
		{
			
		}

		public class Provider : SystemProvider
		{
			public override void GetComponents(out ComponentType[] entityComponents, out ComponentType[] excludedStreamerComponents)
			{
				excludedStreamerComponents = null;
				entityComponents = new ComponentType[]
				{
					typeof(ActionDescription),
					typeof(ActionTag),
					typeof(StActionSlotInput),
					typeof(ProPulseGunAction),
					typeof(ActionAmmo),
					typeof(ActionSlot),
					typeof(ActionCooldown),
					typeof(GenerateEntitySnapshot)
				};
			}

			public Entity SpawnLocal(Entity owner, int slot)
			{
				var action = SpawnLocal();

				EntityManager.ReplaceOwnerData(action, owner);

				EntityManager.SetComponentData(action, new ActionSlot(slot));
				EntityManager.SetComponentData(action, new ActionCooldown(0, 600));
				EntityManager.SetComponentData(action, new ActionAmmo(1500, 3000));
				EntityManager.SetComponentData(action, new ProPulseGunAction
				{
					StartDamage = 10,
					DecreaseValue = 2,
					MaxDecrease = 3
				});

				return action;
			}
		}
	}
}