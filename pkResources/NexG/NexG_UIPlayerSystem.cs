using Stormium.Default.States;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;

namespace Stormium.Default.NexG
{
	public abstract class NexG_UIPlayerSystem : GameBaseSystem
	{
		private EntityQuery m_LocalCameraQuery;
		
		protected override void OnCreateManager()
		{
			base.OnCreateManager();

			m_LocalCameraQuery = GetEntityQuery(typeof(LocalCameraState));
		}

		protected override void OnUpdate()
		{
			var currentGamePlayer = GetFirstSelfGamePlayer();

			LocalCameraState localState = default;
			Entity spectated = default;

			using (var chunkArray = m_LocalCameraQuery.CreateArchetypeChunkArray(Allocator.TempJob))
			{
				for (var i = 0; i != chunkArray.Length; i++)
				{
					var localStateArray = chunkArray[i].GetNativeArray(GetArchetypeChunkComponentType<LocalCameraState>());
					for (var j = 0; j != chunkArray[i].Count; j++)
					{
						var cameraState = localStateArray[j];
						if (spectated == default || cameraState.Mode == CameraMode.Forced)
						{
							spectated = cameraState.Target;
						}
					}
				}
			}

			if (EntityManager.HasComponent<ServerCameraState>(currentGamePlayer))
			{
				var serverState = EntityManager.GetComponentData<ServerCameraState>(currentGamePlayer);
				if (serverState.Mode == CameraMode.Forced || spectated == default)
				{
					spectated = serverState.Target;
				}
			}
			
			OnUpdate(spectated);
		}

		protected abstract void OnUpdate(Entity spectated);
	}
}