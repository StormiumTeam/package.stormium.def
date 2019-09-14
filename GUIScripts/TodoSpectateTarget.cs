namespace GUIScripts
{
	// todo: use rpc?
	/*public class TodoSpectateTarget : GameBaseSyncMessageSystem, INativeEventOnGUI
	{
		public PatternResult SetSpectatorRequestId;
		
		protected override void OnCreate()
		{
			base.OnCreate();

			SetSpectatorRequestId = AddMessage(OnSetSpectatorRequest);
			
			World.GetOrCreateSystem<AppEventSystem>().SubscribeToAll(this);
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
			var snapshot = World.GetExistingSystem<NetworkSnapshotManager>().CurrentSnapshot;

			m_CurrentGamePlayer = GetFirstSelfGamePlayer();
			
			if (m_CurrentGamePlayer == default || !EntityManager.HasComponent<ServerCameraState>(m_CurrentGamePlayer))
				return;

			GUILayout.Space(5);
			GUILayout.Label("SpectateTarget System");
			using (new GUILayout.VerticalScope())
			{
				// Only spectate entity with a camera modifier (and that can generate a snapshot)
				Entities.ForEach((Entity entity, OpenCharacterController controller) =>
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
	}*/
}