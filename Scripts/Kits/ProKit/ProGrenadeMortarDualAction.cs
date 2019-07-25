using Stormium.Core;
using Stormium.Default.Kits.ProKit.ProGrenade;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using Unity.Entities;
using UnityEngine;

namespace Scripts.Kits.ProKit
{
	public struct ProGrenadeMortarDualAction : IComponentData
	{
		public class System : GameBaseSystem
		{
			protected override void OnUpdate()
			{
				Entities.WithAll<ProGrenadeMortarDualAction>().ForEach((DynamicBuffer<MultipleAction> multipleActions) =>
				{
					if (multipleActions.Length != 2)
					{
						Debug.LogError("");
						return;
					}

					var target = default(Entity);
					
					for (var i = 0; i != multipleActions.Length; i++)
					{
						var action = multipleActions[i].Action;
						if (action == default) // this could happen if we don't have the mortar module in the weapon for example
							continue;

						var actionInput = EntityManager.GetComponentData<StActionSlotInput>(action);
						if (actionInput.IsActive)
						{
							target = action;

							break;
						}
					}

					if (target == null)
						return;

					var shootEvent = PostUpdateCommands.CreateEntity();
					if (EntityManager.HasComponent<ProGrenadeAction>(target))
					{
						PostUpdateCommands.AddComponent(shootEvent, new ProGrenadeAction.ShootEvent {Target = target});
					}
				});
			}
		}
	}
}