using NoesisGUIExtensions;
using Revolution.NetCode;
using Stormium.Default.Client.Visual.Interfaces;
using Stormium.Default.Mixed.GameModes;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace IGPlayerState_blend.Unity
{
	public class NoesisElement
	{
		
	}
	
	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	[AlwaysUpdateSystem]
	public class PlayerStateInterfaceSpawner : GameBaseSystem
	{
		public struct AsyncData
		{}
		
		public HudElement Hud;
		
		private AsyncOperationModule m_AsyncOpModule;
		
		protected override void OnCreate()
		{
			base.OnCreate();
			
			GetModule(out m_AsyncOpModule);
			m_AsyncOpModule.Add(Addressables.LoadAssetAsync<NoesisXaml>("def/visuals/Interfaces/IGPlayerState/PlayerStateInterfaceControl.asset"), new AsyncData());
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
						Name             = "PlayerState",
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

			Hud.Active = CanAccessToHud(out var currPlayer, out var cameraState);
			if (Hud.Active)
			{
				(Hud.Content as PlayerStateInterfaceControl).OnUpdate(currPlayer, cameraState);
			}
		}

		private bool CanAccessToHud(out Entity localPlayer, out CameraState cameraState)
		{
			cameraState = default;
			
			localPlayer= GetFirstSelfGamePlayer();
			if (localPlayer == default)
				return false;

			return TryGetCurrentCameraState(localPlayer, out cameraState);
		}
	}

	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	public class PlayerStateInterface : ComponentSystem
	{
		protected override void OnUpdate()
		{

		}
	}

	public partial class PlayerStateInterfaceControl
	{
		public World World;
	}
}