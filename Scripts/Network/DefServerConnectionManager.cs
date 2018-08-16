using DefaultNamespace;
using GameImplementation;
using LiteNetLib;
using LiteNetLib.Utils;
using package.stormiumteam.networking;
using package.stormiumteam.networking.game;
using package.stormiumteam.shared;
using package.stormiumteam.shared.online;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Assertions;

namespace package.stormium.def.Network
{
    public class DefServerConnectionManager : NetworkConnectionSystem,
                                              EventConnectionRequest.IEv,
                                              EventReceiveData.IEv,
                                              EventUserStatusChange.IEv
    {
        private MessageIdent m_MsgUpdatePlayer = new MessageIdent("default.serverconnection_events.update_player");
        private MessageIdent m_MsgRemovePlayer = new MessageIdent("default.serverconnection_events.remove_player");

        [Inject] private ConnectionPatternManager m_ConnectionPatternManager;
        [Inject] private ConnectionPlayerBank     m_ConnectionPlayerBank;

        protected override void OnCreateManager(int capacity)
        {
            MainWorld.GetOrCreateManager<AppEventSystem>().SubscribeToAll(this);

            if (NetInstance.ConnectionInfo.ConnectionType != ConnectionType.Self)
                return;

            m_ConnectionPatternManager.RegisterPattern(m_MsgUpdatePlayer);
            m_ConnectionPatternManager.RegisterPattern(m_MsgRemovePlayer);
        }

        protected override void OnUpdate()
        {

        }

        protected override void OnDestroyManager()
        {
            var em         = MainWorld.GetOrCreateManager<EntityManager>();
            var gpSystem   = MainWorld.GetOrCreateManager<GamePlayerSystem>();
            var allPlayers = gpSystem.SlowGetAllPlayers();
            for (int i = 0; i != allPlayers.Length; i++)
            {
                var entity = allPlayers.Entities[i];
                if (!em.HasComponent<PlayerPeerLink>(entity)) continue;

                var plLink = em.GetSharedComponentData<PlayerPeerLink>(entity);

                if (plLink.Owner != NetInstance.PeerInstance) continue;

                em.RemoveComponent<PlayerPeerLink>(entity);

                if (em.HasComponent<PlayerUserLink>(entity))
                    em.RemoveComponent<PlayerUserLink>(entity);
                if (em.HasComponent<ConnectedPlayerEntity>(entity))
                    em.RemoveComponent<ConnectedPlayerEntity>(entity);
            }
        }

        /// <summary>
        /// On broadcasting data, send all connected players to the new peer
        /// </summary>
        /// <param name="peerInstance"></param>
        public override void OnInstanceBroadcastingData(NetPeerInstance peerInstance)
        {
            if (!NetInstance.SelfHost) return;

            var chanManager = NetInstance.GetChannelManager();
            var plBank      = MainWorld.GetOrCreateManager<GamePlayerBank>();
            var gpSystem    = MainWorld.GetOrCreateManager<GamePlayerSystem>();
            var em          = MainWorld.GetOrCreateManager<EntityManager>();

            var connectedPlayers = gpSystem.SlowGetAllConnectedPlayers();
            for (int i = 0; i != connectedPlayers.Length; i++)
            {
                var entity = connectedPlayers.Entities[i];
                if (em.HasComponent<PlayerPeerLink>(entity))
                {
                    var dataWriter = CreatePlayerDataPacket(entity, (NetPeer) peerInstance);
                    chanManager.DefaultChannel.Manager.SendToAll(dataWriter, DeliveryMethod.ReliableOrdered);
                }
            }
        }

        /// <summary>
        /// Create a packet who contains data about the player
        /// </summary>
        /// <param name="playerEntity"></param>
        /// <param name="owned"></param>
        /// <returns></returns>
        private NetDataWriter CreatePlayerDataPacket(Entity playerEntity, NetPeer peer)
        {
            var em         = MainWorld.GetOrCreateManager<EntityManager>();
            var peerLink   = em.GetSharedComponentData<PlayerPeerLink>(playerEntity);
            var userLink   = em.GetSharedComponentData<PlayerUserLink>(playerEntity);
            var masterLink = em.GetSharedComponentData<MasterServerPlayerId>(playerEntity);

            var msgManager = NetInstance.GetMessageManager();
            var dataWriter = msgManager.Create(m_MsgUpdatePlayer);
            // The index is used to determin the id of the entity in the server
            dataWriter.Put(StMath.DoubleIntToLong(playerEntity.Index, playerEntity.Version));
            dataWriter.Put(userLink.Target.Index);
            dataWriter.Put(masterLink.Id);
            dataWriter.Put(peerLink.Target == peer);

            return dataWriter;
        }

        private NetDataWriter CreateRemovePlayerPacket(Entity playerEntity)
        {
            var em = MainWorld.GetOrCreateManager<EntityManager>();

            var msgManager = NetInstance.GetMessageManager();
            var dataWriter = msgManager.Create(m_MsgRemovePlayer);
            dataWriter.Put(playerEntity.Index);

            return dataWriter;
        }

        void EventReceiveData.IEv.Callback(EventReceiveData.Arguments args)
        {
            var caller       = args.Caller;
            var peerInstance = args.PeerInstance;
            var reader       = args.Reader;

            if (caller != NetInstance || reader.Type != MessageType.Pattern) return;

            var peerPatternMgr = peerInstance.GetPatternManager();
            var msg            = peerPatternMgr.GetPattern(reader);

            var peerNetId     = peerInstance.Global.Id;
            var conPlayerBank = peerInstance.Get<ConnectionPlayerBank>();
            var plBank        = MainWorld.GetOrCreateManager<GamePlayerBank>();
            var em            = MainWorld.GetOrCreateManager<EntityManager>();

            if (msg == m_MsgUpdatePlayer)
            {
                var playerId  = reader.Data.GetEntity();
                var userIndex = reader.Data.GetULong();
                var masterId  = reader.Data.GetIdent128();
                var owned     = reader.Data.GetBool();

                Debug.Log($"Update player ({playerId.Index}) {userIndex} {masterId}.");

                var player = plBank.GetPlayerFromIdent(masterId);
                if (!player.IsCreated)
                {
                    player = new GamePlayer(em.CreateEntity());
                    var masterServerPlayerId = new MasterServerPlayerId()
                    {
                        Id      = masterId,
                        IsLocal = owned
                    };

                    player.WorldPointer.SetOrAddSharedComponentData(masterServerPlayerId, MainWorld);
                    plBank.AddPlayer(masterServerPlayerId, player);
                }
                else
                {
                    var masterServerPlayerId = new MasterServerPlayerId()
                    {
                        Id      = masterId,
                        IsLocal = owned
                    };
                    player.WorldPointer.SetOrAddSharedComponentData(masterServerPlayerId, MainWorld);
                }

                conPlayerBank.RegisterPlayer(StMath.DoubleIntToLong(playerId.Index, playerId.Version), player);

                player.WorldPointer.SetOrAddSharedComponentData
                (
                    new PlayerPeerLink(caller.PeerInstance, peerInstance.Peer),
                    MainWorld
                );
                player.WorldPointer.SetOrAddSharedComponentData
                (
                    new PlayerUserLink(peerInstance.Id, peerNetId, userIndex),
                    MainWorld
                );
                player.WorldPointer.SetOrAddComponentData(new ConnectedPlayerEntity(), MainWorld);
            }
            else if (msg == m_MsgRemovePlayer)
            {
                var playerId = reader.Data.GetInt();
                var player   = m_ConnectionPlayerBank.Get(playerId);
                if (player.IsCreated)
                {
                    em.RemoveComponent<PlayerPeerLink>(player.WorldPointer);
                    em.RemoveComponent<PlayerUserLink>(player.WorldPointer);
                    em.RemoveComponent<ConnectedPlayerEntity>(player.WorldPointer);

                    m_ConnectionPlayerBank.UnregisterPlayer(playerId);

                    Debug.Log("player disconnected!");
                }
                else
                {
                    Debug.LogError("Data incoherence (player.IsCreated == false)");
                }
            }
        }

        void EventUserStatusChange.IEv.Callback(EventUserStatusChange.Arguments args)
        {
            var caller = args.Caller;
            var user   = args.User;
            var change = args.Change;

            if (!NetInstance.SelfHost || caller.Global.Main != NetInstance) return;

            var chanManager = NetInstance.GetChannelManager();
            var plBank      = MainWorld.GetOrCreateManager<GamePlayerBank>();
            var gpSystem    = MainWorld.GetOrCreateManager<GamePlayerSystem>();
            var em          = MainWorld.GetOrCreateManager<EntityManager>();
            
            var player = new GamePlayer();
            foreach (var bankPlayer in plBank.AllPlayers)
            {
                if (em.HasComponent<PlayerPeerLink>(bankPlayer.WorldPointer))
                {
                    var target = em.GetSharedComponentData<PlayerPeerLink>(bankPlayer.WorldPointer).Target;
                    if (target == (NetPeer) user.GetPeerInstance())
                    {
                        player = bankPlayer;
                        break;
                    }
                }
            }

            if (!player.IsCreated)
            {
                Debug.Log("Player wasn't found");
                return;
            }

            if ((change & StatusChange.Removed) != 0)
            {
                if (player.WorldPointer.HasComponent<PlayerUserLink>(MainWorld))
                    em.RemoveComponent<PlayerUserLink>(player.WorldPointer);
                if (player.WorldPointer.HasComponent<PlayerPeerLink>(MainWorld))
                    em.RemoveComponent<PlayerPeerLink>(player.WorldPointer);
                if (player.WorldPointer.HasComponent<ConnectedPlayerEntity>(MainWorld))
                    em.RemoveComponent<ConnectedPlayerEntity>(player.WorldPointer);

                var manager    = chanManager.DefaultChannel.Manager;
                var dataWriter = CreateRemovePlayerPacket(player.WorldPointer);
                foreach (var otherPeer in manager)
                {
                    otherPeer.Send(dataWriter, DeliveryMethod.ReliableOrdered);
                }
            }

            if ((change & StatusChange.Added) != 0)
            {
                player.WorldPointer.SetOrAddSharedComponentData(new PlayerUserLink(user), MainWorld);
                player.WorldPointer.SetOrAddComponentData(new ConnectedPlayerEntity(), MainWorld);

                var dataWriter = CreatePlayerDataPacket(player.WorldPointer, (NetPeer) user.GetPeerInstance());
                var manager    = chanManager.DefaultChannel.Manager;
                foreach (var otherPeer in manager)
                {
                    if (otherPeer == caller.Peer)
                        continue;

                    otherPeer.Send(dataWriter, DeliveryMethod.ReliableOrdered);
                }
            }
        }

        void EventConnectionRequest.IEv.Callback(EventConnectionRequest.Arguments args)
        {
            var caller  = args.Caller;
            var request = args.Request;

            if (caller != NetInstance || !NetInstance.SelfHost) return;

            // TODO: This is only temporary, this should be moved to somewhere else
            var playerId = request.Data.GetIdent128();

            var em     = MainWorld.GetOrCreateManager<EntityManager>();
            var plBank = MainWorld.GetOrCreateManager<GamePlayerBank>();
            var player = plBank.GetPlayerFromIdent(playerId);
            if (!player.IsCreated)
            {
                player = new GamePlayer(em.CreateEntity());
                var masterServerPlayerId = new MasterServerPlayerId()
                {
                    Id      = playerId,
                    IsLocal = false
                };
                
                em.SetOrAddSharedComponentData(player.WorldPointer, masterServerPlayerId);
                plBank.AddPlayer(masterServerPlayerId, player);
            }
            else
            {
                var masterServerPlayerId = new MasterServerPlayerId()
                {
                    Id      = playerId,
                    IsLocal = false
                };

                em.SetOrAddSharedComponentData(player.WorldPointer, masterServerPlayerId);
            }

            m_ConnectionPlayerBank.RegisterPlayer(StMath.DoubleIntToLong(player.WorldPointer.Index, player.WorldPointer.Version), player);

            var peer = request.Accept();
            em.SetOrAddSharedComponentData(player.WorldPointer, new PlayerPeerLink(caller.PeerInstance, peer));
        }
    }
}