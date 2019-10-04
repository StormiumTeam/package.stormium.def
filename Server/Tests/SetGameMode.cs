using Revolution.NetCode;
using Stormium.Default.Mixed.GameModes;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Networking.Transport;

namespace Stormium.Default.Server.Tests
{
	[UpdateInGroup(typeof(ServerInitializationSystemGroup))]
	public class SetGameMode : ComponentSystem
	{
		protected override void OnCreate()
		{
			var gameModeMgr = World.GetOrCreateSystem<GameModeManager>();
			gameModeMgr.SetGameMode(new DeathMatchGameMode(), "DeathMatch");
			
			// start server...
			var networkSystem = World.GetOrCreateSystem<NetworkStreamReceiveSystem>();
			var ep = NetworkEndPoint.LoopbackIpv4;
			ep.Port = 5250;
			networkSystem.Listen(ep);
		}

		protected override void OnUpdate()
		{
		}
	}
}