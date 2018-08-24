using DefaultNamespace;
using LiteNetLib;
using LiteNetLib.Utils;
using package.stormium.core;
using package.stormium.def.characters;
using package.stormium.def.Network;
using package.stormiumteam.networking;
using package.stormiumteam.networking.ecs;
using package.stormiumteam.shared;
using package.stormiumteam.shared.online;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace package.stormium.def
{
    [UpdateBefore(typeof(UpdateRigidbodySystem))]
    public class DefStMvRunInputSystem : ComponentSystem,
                                         EventReceiveData.IEv
    {
        public readonly MessageIdent MsgClientJump, MsgClientWallDodge, MsgClientRunDirection;
        
        [Inject] private GameServerManagement m_GameServerManagement;
        [Inject] private MsgIdRegisterSystem  m_MsgRegisterSystem;
        [Inject] private AppEventSystem       m_AppEventSystem;
        [Inject] private Group m_Group;
        
        protected override void OnCreateManager(int capacity)
        {
            m_AppEventSystem.SubscribeToAll(this);
            m_MsgRegisterSystem.Register(this);
        }

        protected override void OnUpdate()
        {
            var isHost     = m_GameServerManagement.IsCurrentlyHosting;
            var isInServer = m_GameServerManagement.ServerReady;

            var debug = GameObject.Find("use_debug");

            using (var cmd = new EntityCommandBuffer(Allocator.Temp))
            {
                for (var i = 0; i != m_Group.Length; i++)
                {
                    if (!isHost && isInServer)
                    {
                        var mainPlayer = World.GetOrCreateManager<GamePlayerBank>().MainPlayer;
                        var owner      = EntityManager.GetSharedComponentData<MasterServerPlayerId>(m_Group.Owners[i].Target);
                        if (m_Group.Owners[i].Target != mainPlayer.WorldPointer)
                            continue;

                        SendInputs(m_Group.Entities[i].ToEntity());
                    }
                    else if (isHost)
                    {
                    }
                    else if (debug)
                    {
                        Debug.Log("kek");

                        var input = m_Group.Inputs[i];
                        input.RunDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
                        input.Dodge        = Input.GetAxisRaw("Sprint");
                        input.Jump         = Input.GetButtonDown("Jump") ? 1 : input.Jump;
                        input.WallDodge    = Input.GetButtonDown("Sprint") ? 1 : input.WallDodge;

                        cmd.SetComponent(m_Group.Entities[i].ToEntity(), input);
                    }
                }
                
                cmd.Playback(EntityManager);
            }
        }

        private void SendInputs(Entity target)
        {
            var localInstance = m_GameServerManagement.Main.LocalInstance;
            var msgMgr        = localInstance.GetMessageManager();

            NetDataWriter msg = null;
            if (Input.GetButtonDown("Jump"))
            {
                msg = msgMgr.Create(MsgClientJump);
                msg.Put(target);
                msg.Put(Time.frameCount);
                
                m_GameServerManagement.Main.LocalNetManager.SendToAll(msg, DeliveryMethod.ReliableUnordered);
                
                foreach (var manager in AppEvent<IDefStCharacterOnJump>.eventList)
                {
                    AppEvent<IDefStCharacterOnJump>.Caller = this;
                    manager.CharacterOnJump(target);
                }
            }
            if (Input.GetButtonDown("Sprint"))
            {
                msg = msgMgr.Create(MsgClientWallDodge);
                msg.Put(target);
                msg.Put(Time.frameCount);
                
                m_GameServerManagement.Main.LocalNetManager.SendToAll(msg, DeliveryMethod.ReliableUnordered);
            }

            msg = msgMgr.Create(MsgClientRunDirection);
            msg.Put(target);
            msg.Put(Time.frameCount);
            msg.Put(new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")));
            msg.Put(Input.GetAxisRaw("Sprint"));

            m_GameServerManagement.Main.LocalNetManager.SendToAll(msg, DeliveryMethod.Unreliable);
        }

        void EventReceiveData.IEv.Callback(EventReceiveData.Arguments args)
        {
            if (args.Reader.Type != MessageType.Pattern) return;

            var patternMgr = args.PeerInstance.GetPatternManager();
            var msg        = patternMgr.GetPattern(args.Reader);

            if (msg == MsgClientRunDirection)
            {
                var entity     = args.Reader.GetEntity();
                var frameCount = args.Reader.Data.GetInt();
                var runDir     = args.Reader.Data.GetVec3();
                var dodge      = args.Reader.Data.GetFloat();

                if (!EntityManager.Exists(entity))
                {
                    Debug.LogError($"<b><color='red'>No Entity with the id({entity.Index}, {entity.Version}) exists.</color></b>");
                    return;
                }

                var input = entity.GetComponentData<DefStMvInput>();
                input.RunDirection = runDir;
                input.Dodge        = dodge;

                entity.SetComponentData(input);
            }
            
            if (msg == MsgClientJump)
            {
                var entity     = args.Reader.GetEntity();
                var frameCount = args.Reader.Data.GetInt();

                if (!EntityManager.Exists(entity))
                {
                    Debug.LogError($"<b><color='red'>No Entity with the id({entity.Index}, {entity.Version}) exists.</color></b>");
                    return;
                }

                var input = entity.GetComponentData<DefStMvInput>();
                input.Jump = 1;

                entity.SetComponentData(input);
            }
            
            if (msg == MsgClientWallDodge)
            {
                var entity     = args.Reader.GetEntity();
                var frameCount = args.Reader.Data.GetInt();

                if (!EntityManager.Exists(entity))
                {
                    Debug.LogError($"<b><color='red'>No Entity with the id({entity.Index}, {entity.Version}) exists.</color></b>");
                    return;
                }

                var input = entity.GetComponentData<DefStMvInput>();
                input.WallDodge = 1;

                entity.SetComponentData(input);
            }
        }

        private struct Group
        {
            public ComponentDataArray<NetworkEntity> Entities;
            public ComponentDataArray<CharacterPlayerOwner> Owners;
            public ComponentDataArray<StCharacter>  Characters;
            public ComponentDataArray<DefStMvInput> Inputs;

            public readonly int Length;
        }
    }
}