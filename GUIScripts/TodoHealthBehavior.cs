using Stormium.Default.States;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
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
		private EntityQuery m_CharacterGroup;

		protected override void OnCreate()
		{
			base.OnCreate();

			m_CharacterGroup = GetEntityQuery
			(
				ComponentType.ReadWrite<CharacterDescription>(),
				ComponentType.ReadWrite<LivableHealth>(),
				ComponentType.ReadWrite<Relative<PlayerDescription>>()
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
			if (!EntityManager.HasComponent<LivableHealth>(m_CameraTarget))
				return;

			Entities.ForEach((TodoHealthBehavior healthBehavior) =>
			{
				var health = EntityManager.GetComponentData<LivableHealth>(m_CameraTarget);

				if (health.Value != healthBehavior.CurrHealth)
				{
					healthBehavior.CurrHealth      = health.Value;
					healthBehavior.HealthText.text = health.Value.ToString();
				}
			});
		}
	}
}