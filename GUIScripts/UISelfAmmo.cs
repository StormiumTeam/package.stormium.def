using package.StormiumTeam.GameBase;
using Stormium.Default.NexG;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace GUIScripts
{
	[RequireComponent(typeof(GameObjectEntity))]
	public class UISelfAmmo : MonoBehaviour
	{
		public TextMeshProUGUI LabelValue, LabelMax;
		public Image AmmoGauge;
		
		public class System : NexG_UIPlayerSystem
		{
			protected override void OnUpdate(Entity spectated)
			{
				if (spectated == default)
					return;
				
				if (!EntityManager.HasComponent<ActionContainer>(spectated))
					return;

				var buffer = EntityManager.GetBuffer<ActionContainer>(spectated);
				if (buffer.Length <= 0)
					return;

				Entity action = default;
				for (var i = 0; i != buffer.Length; i++)
				{
					if (EntityManager.HasComponent<ActionSlot>(buffer[i].Target)
					    && EntityManager.GetComponentData<ActionSlot>(buffer[i].Target).Value == 0)
						action = buffer[i].Target;
				}

				action = buffer[0].Target;
					
				if (!EntityManager.HasComponent<ActionAmmo>(action))
					return;

				var ammo = EntityManager.GetComponentData<ActionAmmo>(action);
				
				Entities.ForEach((UISelfAmmo uiAmmo) =>
				{
					uiAmmo.LabelValue.text = ammo.GetShootLeft().ToString();
					uiAmmo.LabelMax.text = ammo.GetMaxShoot().ToString();
					uiAmmo.AmmoGauge.fillAmount = (float) ammo.Value / ammo.Max;
				});
			}
		}
	}
}