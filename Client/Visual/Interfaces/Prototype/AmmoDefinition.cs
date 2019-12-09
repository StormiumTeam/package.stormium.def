using TMPro;
using UnityEngine;

namespace Stormium.Default.Client.Visual.Interfaces.Prototype
{
	public class AmmoDefinition : MonoBehaviour
	{
		private int m_Value = -1;
		private int m_Max = -1;

		public TextMeshProUGUI Label;

		public void Set(int val, int max)
		{
			if (m_Value == val && m_Max == max)
				return;

			m_Value = val;
			m_Max = max;

			Label.text = $"{m_Value}/{m_Max}";
		}
	}
}