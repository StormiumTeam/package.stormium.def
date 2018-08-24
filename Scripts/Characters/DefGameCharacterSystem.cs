using System;
using System.ComponentModel;
using System.Text;
using DefaultNamespace;
using LiteNetLib;
using LiteNetLib.Utils;
using package.stormium.def.Network;
using package.stormiumteam.networking;
using package.stormiumteam.networking.ecs;
using package.stormiumteam.shared;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

namespace package.stormium.def.characters
{
    public class DefGameCharacterSystem : ComponentSystem
    {
        #region Barriers

        [UpdateAfter(typeof(DefGameCharacterSystem))]
        class Barrier : BarrierSystem
        {
        }

        #endregion

        #region Groups

        /// <summary>
        /// All characters linked to a player
        /// </summary>
        public struct PlayerGroup
        {

        }

        public struct Characters
        {
            public ComponentDataArray<CharacterTag> CharacterTags;
            public EntityArray                      Entities;

            public readonly int Length;
        }

        public struct PlayerCharacters
        {
            public ComponentDataArray<CharacterTag>         CharacterTags;
            public ComponentDataArray<CharacterPlayerOwner> Owners;
            public EntityArray                              Entities;

            public readonly int Length;
        }

        #endregion

        public static MessageIdent MsgSetCharacterTag;
        public static MessageIdent MsgSetPlayerOwner;
        public static MessageIdent MsgDestroyCharacter;

        [Inject] private MsgIdRegisterSystem  m_MsgIdRegisterSystem;
        [Inject] private NetworkMessageSystem m_NetworkMessageSystem;
        [Inject] private GameServerManagement m_GameServerManagement;
        [Inject] private Barrier              m_Barrier;

        public bool HasLinkToPlayer(Entity entity)
        {
            var gr = this.GetComponentGroup();
            return false;
        }

        public NativeArray<Entity> GetPlayerCharacters(Entity entity)
        {
            var list   = new NativeList<Entity>();
            var length = m_PlayerCharacterGroup.CalculateLength();

            var entities     = m_PlayerCharacterGroup.GetEntityArray();
            var playerOwners = m_PlayerCharacterGroup.GetComponentDataArray<CharacterPlayerOwner>();
            for (int i = 0; i != length; i++)
            {
                if (playerOwners[i].Target == entity)
                    list.Add(entities[i]);
            }

            return list;
        }

        private ComponentGroup m_PlayerCharacterGroup;

        protected override void OnCreateManager(int capacity)
        {
            m_PlayerCharacterGroup = GetComponentGroup
            (
                typeof(CharacterTag),
                typeof(CharacterPlayerOwner)
            );

            m_MsgIdRegisterSystem.Register(this);
        }

        protected override void OnUpdate()
        {

        }

        // ----------------------------------------------------------------------------------------------------------------- //
        // ----------------------------------------------------------------------------------------------------------------- //
        //
        // Create character
        //
        // ----------------------------------------------------------------------------------------------------------------- //
        // ----------------------------------------------------------------------------------------------------------------- //
        public void SetCharacter(Entity characterEntity, bool broadcast = true)
        {
            characterEntity.SetOrAddComponentData(new CharacterTag());
            if (broadcast) ServerGlobalBroadcastSetCharacter(characterEntity);
        }

        public void ServerGlobalBroadcastSetCharacter(Entity characterEntity)
        {
            NetworkInstance serverInstance;
            if (!m_GameServerManagement.TryGetHostOrError(out serverInstance)) return;

            foreach (var peer in m_GameServerManagement.Main.LocalNetManager)
            {
                ServerBroadcastSetCharacter(characterEntity, serverInstance, peer);
            }
        }

        public void ServerBroadcastSetCharacter(Entity characterEntity, NetworkInstance server, NetPeer receiver)
        {
            Profiler.BeginSample("Get Managers");
            var connectionMessageSystem = server.GetMessageManager();
            var connectionEntityManager = server.World.GetOrCreateManager<ConnectionEntityManager>();
            Profiler.EndSample();
            Profiler.BeginSample("Networkify");
            connectionEntityManager.Networkify(characterEntity, World);
            Profiler.EndSample();
            
            Profiler.BeginSample("Create Tag");
            var dataWriter = connectionMessageSystem.Create(MsgSetCharacterTag);
            dataWriter.Put(characterEntity);
            Profiler.EndSample();

            Profiler.BeginSample("Send");
            m_NetworkMessageSystem.InstantSendTo
            (
                receiver,
                null,
                dataWriter,
                DeliveryMethod.ReliableOrdered
            );
            Profiler.EndSample();
        }

        // ----------------------------------------------------------------------------------------------------------------- //
        // ----------------------------------------------------------------------------------------------------------------- //
        //
        // Set the player owner component to a character
        //
        // ----------------------------------------------------------------------------------------------------------------- //
        // ----------------------------------------------------------------------------------------------------------------- //
        public void SetPlayerOwner(Entity entity, Entity owner, bool broadcast = true, CmdBuffer buffer = default(CmdBuffer))
        {
            var b = CmdBuffer.Resolve(m_Barrier, buffer);

            b.SetOrAddComponentData(entity, new CharacterPlayerOwner()
            {
                Target = owner
            });
            if (broadcast) ServerGlobalBroadcastSetPlayerOwner(entity, owner, b);
        }

        public void ServerGlobalBroadcastSetPlayerOwner(Entity entity, Entity owner, CmdBuffer buffer = default(CmdBuffer))
        {
            NetworkInstance serverInstance;
            if (!m_GameServerManagement.TryGetHostOrError(out serverInstance)) return;

            foreach (var peer in m_GameServerManagement.Main.LocalNetManager)
            {
                ServerBroadcastSetPlayerOwner(entity, owner, serverInstance, peer, buffer);
            }
        }

        public void ServerBroadcastSetPlayerOwner
        (
            Entity          entity,
            Entity          owner,
            NetworkInstance server,
            NetPeer         receiver,
            CmdBuffer       buffer = default(CmdBuffer)
        )
        {
            var connectionMessageSystem = server.GetMessageManager();
            var connectionEntityManager = server.World.GetOrCreateManager<ConnectionEntityManager>();
            connectionEntityManager.Networkify(entity, World);

            var dataWriter = connectionMessageSystem.Create(MsgSetPlayerOwner);
            dataWriter.Put(entity);
            dataWriter.Put(owner);

            m_NetworkMessageSystem.InstantSendTo
            (
                receiver,
                null,
                dataWriter,
                DeliveryMethod.ReliableOrdered
            );
        }

        public void ServerBroadcastSetPlayerOwner
        (
            Entity          entity,
            NetworkInstance server,
            NetPeer         receiver,
            CmdBuffer       buffer = default(CmdBuffer)
        )
        {
            Profiler.BeginSample("ServerBroadcastSetPlayerOwner");
            Profiler.BeginSample("Get Managers");
            var owner                   = EntityManager.GetComponentData<CharacterPlayerOwner>(entity).Target;
            var connectionMessageSystem = server.GetMessageManager();
            var connectionEntityManager = server.World.GetOrCreateManager<ConnectionEntityManager>();
            Profiler.EndSample();
            Profiler.BeginSample("Networkify");
            connectionEntityManager.Networkify(entity, World);     
            Profiler.EndSample();
            
            Profiler.BeginSample("Send writer");
            var dataWriter = connectionMessageSystem.Create(MsgSetPlayerOwner);
            dataWriter.Put(entity);
            dataWriter.Put(owner);
            Profiler.EndSample();

            Profiler.BeginSample("InstantSendTo");
            m_NetworkMessageSystem.InstantSendTo
            (
                receiver,
                null,
                dataWriter,
                DeliveryMethod.ReliableOrdered
            );
            Profiler.EndSample();
            Profiler.EndSample();
        }

        public void DestroyCharacter(Entity entity)
        {
            ServerGlobalBroadcastDestroyCharacter(entity);
            
            if (EntityManager.HasComponent<Transform>(entity))
            {
                Object.Destroy(EntityManager.GetComponentObject<Transform>(entity).gameObject);
            }
            else
                EntityManager.DestroyEntity(entity);
        }
        
        public void ServerGlobalBroadcastDestroyCharacter(Entity entity)
        {
            NetworkInstance serverInstance;
            if (!m_GameServerManagement.TryGetHostOrError(out serverInstance)) return;

            foreach (var peer in m_GameServerManagement.Main.LocalNetManager)
            {
                ServerBroadcastDestroyCharacter(entity, serverInstance, peer);
            }
        }
        
        public void ServerBroadcastDestroyCharacter
        (
            Entity          entity,
            NetworkInstance server,
            NetPeer         receiver
        )
        {
            var connectionMessageSystem = server.GetMessageManager();
            var dataWriter = connectionMessageSystem.Create(MsgDestroyCharacter);
            dataWriter.Put(entity);

            m_NetworkMessageSystem.InstantSendTo
            (
                receiver,
                null,
                dataWriter,
                DeliveryMethod.ReliableOrdered
            );
        }
    }
}