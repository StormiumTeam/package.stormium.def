using Unity.NetCode;
using Stormium.Default.Client.Visual.Interfaces;
using Stormium.Default.Mixed.GameModes;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace IGDeathMatch_blend.Unity
{
	public class NoesisElement
	{
		
	}

	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	[AlwaysUpdateSystem]
	public class DeathMatchInterfaceSpawner : GameBaseSystem
	{
		public struct AsyncData
		{
		}

		public HudElement Hud;

		private EntityQuery          m_GameModeQuery;
		private EntityQuery          m_PlayerQuery;
		private AsyncOperationModule m_AsyncOpModule;

		protected override void OnCreate()
		{
			base.OnCreate();

			GetModule(out m_AsyncOpModule);
			m_AsyncOpModule.Add(Addressables.LoadAssetAsync<NoesisXaml>("def/visuals/Interfaces/IGDeathMatch/DeathMatchInterfaceControl.asset"), new AsyncData());

			m_GameModeQuery = GetEntityQuery(typeof(DeathMatchGameMode));
			m_PlayerQuery   = GetEntityQuery(typeof(GamePlayer));
		}

		protected override void OnUpdate()
		{
			for (var i = 0; i != m_AsyncOpModule.Handles.Count; i++)
			{
				var get = m_AsyncOpModule.Get<NoesisXaml, AsyncData>(i);
				if (!get.Handle.IsDone)
					continue;

				if (Hud != null)
					Hud.Destroy();

				var hudMgr = World.GetOrCreateSystem<HudManager>();
				{
					Hud = hudMgr.CreateHud(new CreateHudData
					{
						Name             = "DeathMatch",
						Xaml             = get.Generic.Result,
						ActiveOnCreation = true
					});
				}

				m_AsyncOpModule.Handles.RemoveAtSwapBack(i);
				i--;
			}

			if (Hud == null)
			{
				return;
			}

			Hud.Active = !m_GameModeQuery.IsEmptyIgnoreFilter;
			if (Hud.Active)
			{
				var gameModeEntity = m_GameModeQuery.GetSingletonEntity();
				var gmData         = EntityManager.GetComponentData<DeathMatchGameMode>(gameModeEntity);

				var control = (Hud.Content as DeathMatchInterfaceControl);
				if (control != null)
				{
					control.OnUpdate(GetTick(false).Ms, 0, gmData.TimeLimit);
					using (var entities = m_PlayerQuery.ToEntityArray(Allocator.TempJob))
					{
						var playerSpectators = control.ViewModel.PlayerSpectators;
						foreach (var entity in entities)
						{
							PlayerSpectator playerSpectator = null;

							for (var i = 0; i < playerSpectators.Count; i++)
							{
								var player = playerSpectators[i];
								if (player.Entity == entity)
								{
									playerSpectator = player;
									break;
								}
							}

							if (playerSpectator == null)
							{
								var localTag = EntityManager.HasComponent<GamePlayerLocalTag>(entity) ? "[LOCAL] " : string.Empty;
								playerSpectator = new PlayerSpectator {Content = $"{localTag}Player {entity.Index}", Entity = entity};
								
								Debug.Log("Add");
								playerSpectators.Add(playerSpectator);
							}
						}

						for (var i = 0; i < playerSpectators.Count; i++)
						{
							var player    = playerSpectators[i];
							var hasEntity = false;
							foreach (var entity in entities)
							{
								if (player.Entity == entity)
								{
									hasEntity = true;
									break;
								}
							}

							if (!hasEntity)
							{
								var lastIndex = playerSpectators.Count - 1;
								playerSpectators[i--] = playerSpectators[lastIndex];
								playerSpectators.RemoveAt(lastIndex);
								
								Debug.Log("Remove");
							}
						}
					}
				}
			}
		}
	}

	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	public class DeathMatchInterface : ComponentSystem
	{
		protected override void OnUpdate()
		{

		}
	}

	public partial class DeathMatchInterfaceControl
	{
		public World World;
	}
}