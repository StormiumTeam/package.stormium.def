using System.Collections.Generic;
using System.Net;
using GameImplementation;
using LiteNetLib.Utils;
using package.stormiumteam.networking;
using package.stormiumteam.shared.online;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Profiling;

namespace package.stormium.def.Network
{
    public class GameServerManagement : ComponentSystem
    {
        [Inject] private RuntimeGameNetwork m_GameNetwork;

        public GameServer       Main;
        public List<GameServer> ConnectedServers;
        public bool             IsCurrentlyHosting => Main?.ServerInstance.SelfHost ?? false;

        protected override void OnCreateManager(int capacity)
        {
            Main             = null;
            ConnectedServers = new List<GameServer>();
        }

        protected override void OnUpdate()
        {
        }

        public void ConnectToServer(string serverAddress, int serverPort, int clientPort = 0)
        {
            var netManager = World.GetOrCreateManager<NetworkManager>();
            var plBank     = World.GetOrCreateManager<GamePlayerBank>();

            var msg = new NetDataWriter();
            msg.Put(EntityManager.GetSharedComponentData<MasterServerPlayerId>(plBank.MainPlayer.WorldPointer).Id);

            var connectionCreator = new NetworkSelfConnectionCreator()
            {
                ManagerAddress = "127.0.0.1",
                ManagerPort    = (short) clientPort,
            };

            NetworkConnectionCreator.ConnectToNetwork(netManager,
                msg,
                connectionCreator,
                new IPEndPoint(IPAddress.Parse(serverAddress), serverPort),
                out var localInstance,
                out var conInstance);

            Main = GameServer.From(localInstance, conInstance);
            ConnectedServers.Add(Main);
        }

        public void CreateServer(int port)
        {
            var netManager = World.GetOrCreateManager<NetworkManager>();
            var plBank     = World.GetOrCreateManager<GamePlayerBank>();

            var connectionCreator = new NetworkSelfConnectionCreator()
            {
                ManagerAddress = "127.0.0.1",
                ManagerPort    = (short) port,
            };

            NetworkConnectionCreator.CreateNetwork(netManager,
                connectionCreator,
                out var localInstance);

            Main = GameServer.From(localInstance, localInstance);
            ConnectedServers.Add(Main);
        }

        public void Disconnect(GameServer server)
        {
            if (ConnectedServers.Contains(server))
                ConnectedServers.Remove(server);

            server.CloseConnection();

            if (server == Main)
                Main = null;
        }

        public void DisconnectFromAll()
        {
            foreach (var server in ConnectedServers)
                server.CloseConnection();

            ConnectedServers.Clear();

            Main = null;
        }

        public bool TryGetHostOrError(out NetworkInstance serverInstance)
        {
            Profiler.BeginSample("TryGetHostOrError");
            const string cl = "#FF2100";
            
            serverInstance = null;
            Profiler.BeginSample("!GameLaunch.IsServer");
            if (!GameLaunch.IsServer)
            {
                Profiler.EndSample();
                Debug.LogError
                (
                    $"<b>[GMS][Incoherance] <color='{cl}'>We are a client but we were asked to do a server stuff.</color></b>"
                );
                return false;
            }
            Profiler.EndSample();

            Profiler.BeginSample("!IsCurrentlyHosting && GameLaunch.IsServer");
            if (!IsCurrentlyHosting && GameLaunch.IsServer)
            {
                Profiler.EndSample();
                Debug.LogError
                (
                    $"<b>[GMS][Strange Error] <color='{cl}'>We are a server but we aren't currently hosting.</color></b>"
                );
                return false;
            }
            Profiler.EndSample();

            Profiler.BeginSample("Main.ServerInstance != Main.LocalInstance");
            if (Main.ServerInstance != Main.LocalInstance)
            {
                Profiler.EndSample();
                Debug.LogError
                (
                    $"<b>[GMS][Strange Error] <color='{cl}'>We are a server but the server instance and the local instance are not equal</color></b>"
                );
            }
            Profiler.EndSample();

            serverInstance = Main.ServerInstance;
            Profiler.EndSample();
            return true;
        }
        
        public bool TryGetHost(out NetworkInstance serverInstance)
        {
            const string cl = "#FF2100";
            
            serverInstance = null;
            if (!GameLaunch.IsServer)
            {
                return false;
            }

            if (!IsCurrentlyHosting && GameLaunch.IsServer)
            {
                // We must log this
                Debug.LogError
                (
                    $"<b>[GMS][Strange Error] <color='{cl}'>We are a server but we aren't currently hosting.</color></b>"
                );
                return false;
            }

            if (Main.ServerInstance != Main.LocalInstance)
            {
                // We must log this
                Debug.LogError
                (
                    $"<b>[GMS][Strange Error] <color='{cl}'>We are a server but the server instance and the local instance are not equal</color></b>"
                );
                return false;
            }

            serverInstance = Main.ServerInstance;
            return true;
        }
    }
}