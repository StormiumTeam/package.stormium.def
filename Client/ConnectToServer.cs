using System.IO;
using System.Net;
using Unity.NetCode;
using Stormium.Core;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Networking.Transport;
using Unity.UIElements.Runtime;
using UnityEngine;
using UnityEngine.UIElements;

namespace DefaultNamespace
{
	public struct ClientInit
	{
		public string address;
		public ushort port;
		public float  sensivity;
	}

	[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
	public class ConnectToServer : ComponentSystem
	{
		PanelRenderer panelRenderer;
		private Label pingLabel;

		protected override void OnCreate()
		{
			var filePath = Application.streamingAssetsPath + "/c_init.ini";
			if (!File.Exists(filePath))
			{
				File.Create(filePath).Dispose();
				File.WriteAllText(filePath, JsonUtility.ToJson(new ClientInit {address = "127.0.0.1", port = 5250}));
			}

			var init = JsonUtility.FromJson<ClientInit>(File.ReadAllText(filePath));
			if (init.sensivity < 0.1f)
				init.sensivity = 1.0f;

			BasicUserCommandUpdateLocal.sensivity = init.sensivity;

			File.WriteAllText(filePath, JsonUtility.ToJson(init));

			var networkSystem = World.GetOrCreateSystem<NetworkStreamReceiveSystem>();
			var ip            = new IPEndPoint(IPAddress.Parse(init.address), init.port);
			var ep            = NetworkEndPoint.Parse(init.address, init.port);

			networkSystem.Connect(ep);
		}

		protected override void OnUpdate()
		{
		}
	}
}