using System.IO;
using System.Net;
using Unity.NetCode;
using Stormium.Default.Mixed.GameModes;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Networking.Transport;
using UnityEngine;

namespace Stormium.Default.Server.Tests
{
	public struct ServerInit
	{
		public bool init;
	}
	
	[UpdateInGroup(typeof(ServerInitializationSystemGroup))]
	public class SetGameMode : ComponentSystem
	{
		protected override void OnCreate()
		{
			var filePath = Application.streamingAssetsPath + "/s_init.ini";
			if (!File.Exists(filePath))
			{
				File.Create(filePath).Dispose();
				File.WriteAllText(filePath, JsonUtility.ToJson(new ServerInit { init = false }));
			}

			var init = JsonUtility.FromJson<ServerInit>(File.ReadAllText(filePath));
			if (!init.init && !Application.isEditor)
				return;
				
			var gameModeMgr = World.GetOrCreateSystem<GameModeManager>();
			gameModeMgr.SetGameMode(new DeathMatchGameMode(), "DeathMatch");
			
			// start server...
			var networkSystem = World.GetOrCreateSystem<NetworkStreamReceiveSystem>();
			var ep = NetworkEndPoint.AnyIpv4;
			ep.Port = 5250;
			
			networkSystem.Listen(ep);
		}

		protected override void OnUpdate()
		{
		}
	}
}