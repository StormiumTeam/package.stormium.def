using LiteNetLib;
using LiteNetLib.Utils;
using package.stormium.def.Network;
using package.stormiumteam.networking;
using package.stormiumteam.networking.ecs;
using package.stormiumteam.networking.plugins;
using package.stormiumteam.shared;
using Unity.Entities;

namespace package.stormium.def
{
    public abstract class GameComponentSystem : ComponentSystem
    {
        protected ConnectionEntityManager ServerEntityMgr => GameServerManagement
                                                             .Main
                                                             .ServerInstance
                                                             .World
                                                             .GetOrCreateManager<ConnectionEntityManager>();

        protected bool IsConnectedOrHosting => GameServerManagement.Main?.LocalNetManager.IsRunning ?? false;
        
        [Inject] protected MsgIdRegisterSystem MsgIdRegisterSystem;
        [Inject] protected GameServerManagement GameServerManagement;
        [Inject] protected AppEventSystem AppEventSystem;
        
        protected override void OnCreateManager(int capacity)
        {
            MsgIdRegisterSystem.Register(this);
            AppEventSystem.SubscribeToAll(this);
        }

        protected void ServerSendToPeer(NetPeer peer, NetDataWriter data, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
        {
            peer.Send(data, deliveryMethod);
        }

        protected void ServerSendToAll(NetDataWriter data, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
        {
            var manager = GameServerManagement.Main.ServerInstance.GetDefaultChannel().Manager;
            manager.SendToAll(data, deliveryMethod);
        }
    }
}