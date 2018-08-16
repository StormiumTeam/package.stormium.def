using System;
using LiteNetLib;
using package.stormiumteam.networking;
using package.stormiumteam.shared;
using package.stormiumteam.shared.online;
using Unity.Entities;
using UnityEngine;

namespace package.stormium.def.Network
{
    public interface IAppEventGameServerOnNewPlayer : IAppEvent
    {
        void GameServerOnNewPlayer(GamePlayer player);
    }

    public class GameServer
    {
        public NetManager LocalNetManager;
        public NetworkInstance LocalInstance;
        public NetworkInstance ServerInstance;

        public void CloseConnection()
        {
            try
            {
                LocalInstance.Dispose();
                if (LocalInstance != ServerInstance) ServerInstance.Dispose();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                LocalNetManager?.Stop();
                LocalNetManager = null;
                LocalInstance = null;
                ServerInstance = null;
            }
        }

        public static GameServer From(NetworkInstance local, NetworkInstance server)
        {
            return new GameServer()
            {
                LocalInstance  = local,
                ServerInstance = server,
                LocalNetManager = (local.ConnectionInfo.Creator as IConnectionHost)?.Manager
            };
        }
    }
}