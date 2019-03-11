using Stormium.Default.States;
using StormiumTeam.GameBase;
using TMPro;
using Unity.Entities;
using UnityEngine;

namespace GUIScripts
{
	public class TodoHealthBehavior : MonoBehaviour
	{
		public TextMeshProUGUI HealthText;
		public int             CurrHealth;
	}

	public class TodoHealthBehaviorSystem : GameBaseSystem
	{
		private Entity         m_CameraTarget;
		private ComponentGroup m_CharacterGroup;

		protected override void OnCreateManager()
		{
			base.OnCreateManager();

			m_CharacterGroup = GetComponentGroup
			(
				ComponentType.ReadWrite<CharacterDescription>(),
				ComponentType.ReadWrite<HealthState>(),
				ComponentType.ReadWrite<OwnerState<PlayerDescription>>()
			);
		}

		protected override void OnUpdate()
		{
			var gamePlayer = GetFirstSelfGamePlayer();
			if (gamePlayer == default)
				return;

			if (!EntityManager.HasComponent<ServerCameraState>(gamePlayer))
				return;

			m_CameraTarget = EntityManager.GetComponentData<ServerCameraState>(gamePlayer).Target;
			if (!EntityManager.HasComponent<HealthState>(m_CameraTarget))
				return;

			ForEach((TodoHealthBehavior healthBehavior) =>
			{
				var health = EntityManager.GetComponentData<HealthState>(m_CameraTarget);

				if (health.Health != healthBehavior.CurrHealth)
				{
					healthBehavior.CurrHealth      = health.Health;
					healthBehavior.HealthText.text = health.Health.ToString();
				}
			});
		}
	}
}