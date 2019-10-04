using Revolution.NetCode;
using Unity.Entities;
using Unity.Networking.Transport;

namespace DefaultNamespace
{
	[UpdateInGroup(typeof(ClientSimulationSystemGroup))]
	public class ConnectToServer : ComponentSystem
	{
		protected override void OnCreate()
		{
			var networkSystem = World.GetOrCreateSystem<NetworkStreamReceiveSystem>();
			var ep = NetworkEndPoint.LoopbackIpv4;
			ep.Port = 5250;

			networkSystem.Connect(ep);
		}

		protected override void OnUpdate()
		{
			
		}
	}
}