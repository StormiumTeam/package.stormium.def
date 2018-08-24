using DefaultNamespace;
using LiteNetLib;
using package.stormium.core;
using package.stormium.def.characters;
using package.stormium.def.Network;
using package.stormiumteam.networking;
using package.stormiumteam.networking.ecs;
using package.stormiumteam.shared;
using package.stormiumteam.shared.online;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Jobs;

namespace package.stormium.def
{
    [UpdateBefore(typeof(UpdateRigidbodySystem))]
    public class StEntityHeadLookAtSystem : ComponentSystem,
                                             EventReceiveData.IEv
    {
        public readonly MessageIdent MsgServerUpdateLookAt;
        public readonly MessageIdent MsgClientSendMouseLookInput;

        [Inject] private Group                m_Group;
        [Inject] private GameServerManagement m_GameServerManagement;
        [Inject] private MsgIdRegisterSystem  m_MsgRegisterSystem;
        [Inject] private AppEventSystem       m_AppEventSystem;

        protected override void OnCreateManager(int capacity)
        {
            m_AppEventSystem.SubscribeToAll(this);
            m_MsgRegisterSystem.Register(this);
        }

        protected override void OnUpdate()
        {
            var isHost     = m_GameServerManagement.IsCurrentlyHosting;
            var isInServer = m_GameServerManagement.ServerReady;

            using (var cmd = new EntityCommandBuffer(Allocator.Temp))
            {
                for (var i = 0; i != m_Group.Length; i++)
                {
                    var netEntity = m_Group.NetEntities[i];
                    if (!isHost && isInServer)
                    {
                        var netHead   = m_Group.NetCharacterHeads[i];
                        var localHead = m_Group.CharacterHeads[i];

                        var mainPlayer = World.GetOrCreateManager<GamePlayerBank>().MainPlayer;
                        var owner      = EntityManager.GetSharedComponentData<MasterServerPlayerId>(m_Group.Owners[i].Target);
                        if (m_Group.Owners[i].Target != mainPlayer.WorldPointer)
                        {
                            localHead.Rotation[i] = netHead.Rotation[i];
                            cmd.SetComponent(m_Group.Entities[i], localHead);
                            continue;
                        }

                        var lookDelta = new Vector3(-Input.GetAxisRaw("Mouse Y"), Input.GetAxisRaw("Mouse X"), 0);

                        localHead.Rotation *= Quaternion.Euler(lookDelta);

                        var euler = localHead.Rotation.eulerAngles;
                        euler.z            = 0;
                        localHead.Rotation = Quaternion.Euler(euler);

                        //if (Quaternion.Angle(localHead.Rotation, netHead.Rotation) > 0.01f) 
                        ClientSendInputUpdateToServer(netEntity, localHead.Rotation);

                        cmd.SetComponent(m_Group.Entities[i], localHead);
                    }
                    else if (isHost)
                    {
                        ServerSendHeadToClients(netEntity, m_Group.CharacterHeads[i].Rotation);

                        var rotEuler = m_Group.CharacterHeads[i].Rotation.eulerAngles;
                        cmd.SetComponent(m_Group.Entities[i], new Rotation {Value = Quaternion.Euler(0, rotEuler.y, 0)});
                        m_Group.Positions[i].rotation = Quaternion.Euler(0, rotEuler.y, 0);
                    }
                }

                cmd.Playback(EntityManager);
            }
        }

        private void ClientSendInputUpdateToServer(NetworkEntity netEntity, Quaternion lookDelta)
        {
            var localInstance = m_GameServerManagement.Main.LocalInstance;
            var msgMgr        = localInstance.GetMessageManager();
            var msg           = msgMgr.Create(MsgClientSendMouseLookInput);
            msg.Put(netEntity.ToEntity());
            msg.Put(lookDelta.eulerAngles);

            m_GameServerManagement.Main.LocalNetManager.SendToAll(msg, DeliveryMethod.Unreliable);
        }

        private void ServerSendHeadToClients(NetworkEntity netEntity, Quaternion lookDelta)
        {
            var host = m_GameServerManagement.Main.LocalInstance;
            var msgMgr = host.GetMessageManager();
            var msg = msgMgr.Create(MsgServerUpdateLookAt);
            msg.Put(netEntity.ToEntity());
            msg.Put(lookDelta.eulerAngles);
            
            m_GameServerManagement.Main.LocalNetManager.SendToAll(msg, DeliveryMethod.Unreliable);
        }

        void EventReceiveData.IEv.Callback(EventReceiveData.Arguments args)
        {
            if (args.Reader.Type != MessageType.Pattern) return;

            var patternMgr = args.PeerInstance.GetPatternManager();
            var msg        = patternMgr.GetPattern(args.Reader);

            if (msg == MsgClientSendMouseLookInput)
            {
                var entity    = args.Reader.GetEntity();
                var lookDelta = args.Reader.Data.GetVec3();
                
                if (!EntityManager.Exists(entity))
                {
                    Debug.LogError($"<b><color='red'>No Entity with the id({entity.Index}, {entity.Version}) exists.</color></b>");
                    return;
                }

                // TODO: find a way to replace this with Rotation and not Transform
                var tr = EntityManager.GetComponentData<StEntityHeadLookAt>(entity);
                tr.Rotation = Quaternion.Euler(lookDelta);
                EntityManager.SetComponentData(entity, tr);
            }
            else if (msg == MsgServerUpdateLookAt)
            {
                // todo
                return;
                
                var conEntityMgr = args.PeerInstance.Get<ConnectionEntityManager>();
                var entity = conEntityMgr.GetEntity(args.Reader.GetEntity());
                var lookDelta = args.Reader.Data.GetVec3();
                
                if (!EntityManager.Exists(entity))
                {
                    Debug.LogError($"<b><color='red'>No Entity with the id({entity.Index}, {entity.Version}) exists.</color></b>");
                    return;
                }

                var tr = EntityManager.GetComponentData<NetStEntityHeadLookAt>(entity);
                tr.Rotation = Quaternion.Euler(lookDelta);
                EntityManager.SetComponentData(entity, tr);
            }
        }
        
        public static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360F)
                angle += 360F;
            if (angle > 360F)
                angle -= 360F;
            return Mathf.Clamp(angle, min, max);
        }

        private struct Group
        {
            public ComponentDataArray<NetStEntityHeadLookAt> NetCharacterHeads;
            public ComponentDataArray<StEntityHeadLookAt> CharacterHeads;
            public ComponentDataArray<CharacterPlayerOwner> Owners;
            public ComponentDataArray<StCharacter>        Characters;
            public ComponentDataArray<NetworkEntity>      NetEntities;
            public ComponentDataArray<Rotation> Rotations;
            public TransformAccessArray Positions;
            public EntityArray Entities;

            public readonly int Length;
        }
    }
}