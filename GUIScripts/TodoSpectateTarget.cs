using package.stormiumteam.networking;
using package.stormiumteam.networking.runtime.highlevel;
using package.stormiumteam.networking.runtime.lowlevel;
using package.stormiumteam.shared;
using StandardAssets.Characters.Physics;
using Stormium.Core.Networking;
using Stormium.Default.States;
using StormiumShared.Core.Networking;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace GUIScripts
{
	public class TodoSpectateTarget : GameBaseSyncMessageSystem, INativeEventOnGUI
	{
		public PatternResult SetSpectatorRequestId;
		
		protected override void OnCreateManager()
		{
			base.OnCreateManager();

			SetSpectatorRequestId = AddMessage(OnSetSpectatorRequest);
			
			World.GetOrCreateManager<AppEventSystem>().SubscribeToAll(this);
		}

		private void OnSetSpectatorRequest(NetworkInstanceData networkInstance, Entity client, DataBufferReader data)
		{
			var target = data.ReadValue<Entity>();
			var gamePlayer = EntityManager.GetComponentData<NetworkClientToGamePlayer>(client).Target;
			EntityManager.SetComponentData(gamePlayer, new ServerCameraState(target));
		}

		private Entity m_CurrentGamePlayer;

		public void NativeOnGUI()
		{
			var snapshot = World.GetExistingManager<NetworkSnapshotManager>().CurrentSnapshot;

			m_CurrentGamePlayer = GetFirstSelfGamePlayer();
			
			if (m_CurrentGamePlayer == default || !EntityManager.HasComponent<ServerCameraState>(m_CurrentGamePlayer))
				return;

			GUILayout.Space(5);
			GUILayout.Label("SpectateTarget System");
			using (new GUILayout.VerticalScope())
			{
				// Only spectate entity with a camera modifier (and that can generate a snapshot)
				ForEach((Entity entity, OpenCharacterController controller) =>
				{
					if (!EntityManager.HasComponent<GenerateEntitySnapshot>(entity))
						return;

					if (!GUILayout.Button($"Spectate {entity}")) 
						return;
					
					// If we don't own it, it means it's from the server
					if ((GameMgr.GameType & GameType.Server) == 0 && !EntityManager.HasComponent<EntityAuthority>(entity))
					{
						using (var data = new DataBufferWriter(32, Allocator.TempJob))
						{
							data.WriteValue(snapshot.EntityToSnapshot(entity));
							SyncToServer(SetSpectatorRequestId, data);
						}
					}
					else
					{
						EntityManager.SetComponentData(m_CurrentGamePlayer, new ServerCameraState(entity));
					}
				});
			}
			GUILayout.Space(5);
		}
	}
}