using Stormium.Default.States;
using StormiumTeam.GameBase;
using Unity.Entities;

namespace Stormium.Default.NexG
{
	public abstract class NexG_UIPlayerSystem : GameBaseSystem
	{
		protected override void OnUpdate()
		{
			var currentGamePlayer = GetFirstSelfGamePlayer();
			if (currentGamePlayer == default) 
				return;

			LocalCameraState? localState = default;
			ServerCameraState? serverState = default;

			if (EntityManager.HasComponent<LocalCameraState>(currentGamePlayer))
			{
				localState = EntityManager.GetComponentData<LocalCameraState>(currentGamePlayer);
			}
			if (EntityManager.HasComponent<ServerCameraState>(currentGamePlayer))
			{
				serverState = EntityManager.GetComponentData<ServerCameraState>(currentGamePlayer);
			}

			if (serverState.HasValue)
			{
				if (serverState.Value.Mode == CameraMode.Forced)
					OnUpdate(serverState.Value.Target);
				else if (localState.HasValue && localState.Value.Mode == CameraMode.Forced)
					OnUpdate(localState.Value.Target);
			}
			else if (localState.HasValue)
			{
				OnUpdate(localState.Value.Target);
			}
		}

		protected abstract void OnUpdate(Entity spectated);
	}
}