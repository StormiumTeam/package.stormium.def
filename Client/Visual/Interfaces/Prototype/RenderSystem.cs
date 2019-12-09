using System;
using StormiumTeam.GameBase;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Stormium.Default.Client.Visual.Interfaces.Prototype
{
	[Serializable]
	public class GaugeDefinition : MonoBehaviour
	{
		public bool IsDirty { get; set; } = true;

		[SerializeField]
		private Image gauge;

		[SerializeField]
		private TextMeshProUGUI labelValue, labelMax;

		private int m_Value, m_Max;

		public void Set(int value, int max)
		{
			IsDirty = !m_Value.Equals(value)
			          || !m_Max.Equals(max);

			m_Value = value;
			m_Max   = max;
		}

		private void LateUpdate()
		{
			/*if (!IsDirty)
				return;
			IsDirty = false;*/

			gauge.fillAmount = m_Value > 0 && m_Max > 0
				? (float) m_Value / (float) m_Max
				: 0;
			labelValue.text = m_Value.ToString();
			labelMax.text   = m_Max.ToString();
		}
	}

	public abstract class RenderSystem<TDefinition> : GameBaseSystem
		where TDefinition : Component
	{
		public abstract void PrepareValues();
		public abstract void Render(TDefinition definition);
		public abstract void ClearValues();

		protected override void OnUpdate()
		{
			PrepareValues();
			Entities.ForEach((TDefinition definition) =>
			{
				Render(definition);
			});
			ClearValues();
		}
	}
}