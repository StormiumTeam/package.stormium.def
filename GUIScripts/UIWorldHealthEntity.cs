using StormiumTeam.GameBase.Components;
using TMPro;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace GUIScripts
{
	[RequireComponent(typeof(GameObjectEntity))]
	public class UIWorldHealthEntity : MonoBehaviour
	{
		public Entity Target;

		public float InterpolationLength = 0.1f;

		public Image           Gauge;
		public TextMeshProUGUI HealthValue, HealthMax;

		private int m_LastValue = -1, m_LastMax = -1;

		private float m_InterpolationStartValue, m_InterpolationEndValue;
		private float m_InterpolationTime;

		private void Awake()
		{
			RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
		}

		private void OnDestroy()
		{
			RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
		}

		private void OnBeginCameraRendering(ScriptableRenderContext ctx, Camera obj)
		{
			var camTransform = obj.transform;

			transform.LookAt(transform.position + camTransform.rotation * Vector3.forward, camTransform.rotation * Vector3.up);
		}

		private void Set(int value, int max)
		{
			m_InterpolationStartValue = math.isfinite(Gauge.fillAmount) ? Gauge.fillAmount : 0.0f;
			m_InterpolationTime = 0.0f;
			
			m_InterpolationEndValue = (float) value / (float) max;
			if (float.IsInfinity(m_InterpolationEndValue)) 
				m_InterpolationEndValue = 0.0f;

			if (m_LastValue != value) HealthValue.text = value.ToString();
			if (m_LastMax != max) HealthMax.text       = max.ToString();

			m_LastValue = value;
			m_LastMax   = max;
		}

		private void SystemUpdate()
		{
			m_InterpolationTime += Time.deltaTime;
			if (m_InterpolationTime > InterpolationLength)
			{
				m_InterpolationTime = InterpolationLength;
			}
			
			var p = m_InterpolationTime / InterpolationLength;

			Gauge.fillAmount = Mathf.Lerp(m_InterpolationStartValue, m_InterpolationEndValue, p);
		}

		public class System : ComponentSystem
		{
			protected override void OnUpdate()
			{
				Entities.ForEach((UIWorldHealthEntity uiHealth) =>
				{
					if (uiHealth.Target == default)
						return;

					var livableHealth = EntityManager.GetComponentData<LivableHealth>(uiHealth.Target);

					uiHealth.Set(livableHealth.Value, livableHealth.Max);
					uiHealth.SystemUpdate();
				});
			}
		}
	}
}