using UnityEngine;
using UnityEngine.UI;

namespace Stormium.Default.Client.Visual.Interfaces.Prototype
{
	public class ReticleDefinition : MonoBehaviour
	{
		private static readonly int s_Active = Animator.StringToHash("Active");

		public Animator Animator;
		public Image    Gauge;

		private bool m_Active;

		public void SetActive(bool value)
		{
			if (m_Active == value)
				return;

			Animator.SetBool(s_Active, m_Active = value);
		}

		public void SetProgression(int val, int max)
		{
			Gauge.fillAmount = (float) val / max;
		}
	}
}