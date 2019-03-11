using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Stormium.Default.NexG
{
	public class NexG_Stamina : MonoBehaviour
	{
		private float m_CurrStaminaValue = -1;
		
		public TextMeshProUGUI ProgressLabel;
		public Image GaugeQuad;

		public int SetStaminaValue(float newValue)
		{
			if (m_CurrStaminaValue.Equals(newValue))
				return 0;

			m_CurrStaminaValue = newValue;
			return 1;
		}

		public void NexG_Update(int dirtyCount)
		{
			if (dirtyCount == 0)
				return;

			ProgressLabel.text = $"{m_CurrStaminaValue}%";
			GaugeQuad.fillAmount = m_CurrStaminaValue * 0.01f;
		}
	}
	
	public class NexG_StaminaSystem : NexG_UIPlayerSystem
	{
		protected override void OnUpdate(Entity spectated)
		{
		}
	}
}
