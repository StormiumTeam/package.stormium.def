using System;
using System.IO;
using Revolution.NetCode;
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
				File.WriteAllText(filePath, JsonUtility.ToJson(new ClientInit { address = "127.0.0.1", port = 5250 }));
			}

			var init = JsonUtility.FromJson<ClientInit>(File.ReadAllText(filePath));
			
			var networkSystem = World.GetOrCreateSystem<NetworkStreamReceiveSystem>();
			var ep            = NetworkEndPoint.Parse(init.address, init.port);

			networkSystem.Connect(ep);
		}

		protected override void OnUpdate()
		{
		}
	}
}