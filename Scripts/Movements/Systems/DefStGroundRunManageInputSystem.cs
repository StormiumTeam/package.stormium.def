using System;
using DefaultNamespace;
using LiteNetLib;
using LiteNetLib.Utils;
using package.stormium.def.Movements.Data;
using package.stormium.def.Network;
using package.stormiumteam.networking;
using package.stormiumteam.networking.ecs;
using package.stormiumteam.shared;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Input;

namespace package.stormium.def.Movements.Systems
{
    public class DefStGroundRunManageInputSystem : GameComponentSystem,
                                                   EventReceiveData.IEv
    {
        private static readonly MessageIdent InputMsgToServerId;
        private static readonly MessageIdent InputMsgToClientsId;

        struct Group
        {
            public ComponentDataArray<StCharacter>   Characters;
            public ComponentDataArray<NetworkEntity> NetworkEntities;
            public ComponentDataArray<DefStGroundRunInput> Inputs;

            public readonly int Length;
        }

        [Inject] private Group m_Group;

        [Inject] private NetworkMessageSystem m_NetworkMessageSystem;

        private DefStGroundRunManageInputClient m_InputClient;

        protected override void OnCreateManager(int capacity)
        {
            base.OnCreateManager(capacity);

            m_InputClient = new DefStGroundRunManageInputClient();
            m_InputClient.CreateActionMap();
            m_InputClient.Enable();
        }
 
        protected override void OnUpdate()
        {
            var currentInput = m_InputClient.CurrentInput;
            for (int i = 0; i != m_Group.Length; i++)
            {
                var netEntity = m_Group.NetworkEntities[i];

                InputPacket packet;
                if (GameServerManagement.IsCurrentlyHosting)
                {
                    packet = new InputPacket(netEntity.ToEntity(), m_Group.Inputs[i].Direction)
                    {
                        Timestamp = m_Group.Inputs[i].Timestamp
                    };

                    SendNewInputToClients(netEntity.GetNetworkInstance(), packet);   
                }
                else
                {
                    packet = new InputPacket(netEntity.ToEntity(), currentInput.Direction);

                    SendNewInputToServer(netEntity, packet);

                    m_Group.Inputs[i] = new DefStGroundRunInput(packet.Timestamp, packet.Direction);
                }
            }
        }

        protected override void OnDestroyManager()
        {
            m_InputClient.Disable();
        }

        void EventReceiveData.IEv.Callback(EventReceiveData.Arguments args)
        {
            if (args.Reader.Type != MessageType.Pattern)
                return;
            
            var conPatternMgr = args.PeerInstance.GetPatternManager();
            var conEntityMgr = args.PeerInstance.Get<ConnectionEntityManager>();
            var msgPattern    = conPatternMgr.GetPattern(args.Reader);
            if (msgPattern == InputMsgToServerId)
            {
                var inputPacket = new InputPacket(args.Reader.Data);
                var entity      = inputPacket.Entity;

                Debug.Log($"{Time.frameCount} >> Received package >> {inputPacket.Timestamp}");

                if (!EntityManager.Exists(entity))
                {
                    //TODO: Disconnect player
                    Debug.Log($"Invalid entity ({entity.Index}, {entity.Version}) ! TODO: Disconnect Player");
                    return;
                }

                if (entity.HasComponent<DefStGroundRunInput>())
                {
                    entity.SetComponentData(new DefStGroundRunInput(inputPacket.Timestamp, inputPacket.Direction));
                }
                else
                {
                    //TODO: Disconnect player
                    Debug.Log($"Invalid component for ({entity.Index}, {entity.Version}) ! TODO: Disconnect Player");
                }
            }
            else if (msgPattern == InputMsgToClientsId)
            {
                var inputPacket = new InputPacket(args.Reader.Data);
                var entity      = inputPacket.Entity;

                if (!conEntityMgr.HasEntity(entity))
                {
                    //TODO: Disconnect from server
                    Debug.Log($"Invalid entity ({entity.Index}, {entity.Version}) ! TODO: Disconnect from server.");
                    return;
                }

                entity = conEntityMgr.GetEntity(entity);

                if (entity.HasComponent<DefStGroundRunInput>())
                {
                    var data = entity.GetComponentData<DefStGroundRunInput>();
                    if (data.Timestamp >= inputPacket.Timestamp) // Ignore
                    {
                        //TODO: this should only be logged for non owned entities
                        Debug.Log($"{Time.frameCount} Current timestamp ({data.Timestamp}) was higher than packet timestamp ({inputPacket.Timestamp})");
                    }
                    else
                    {
                        entity.SetComponentData(new DefStGroundRunInput(inputPacket.Timestamp, inputPacket.Direction));
                    }
                }
                else
                {
                    //TODO: Disconnect from server
                    Debug.Log($"Invalid component for ({entity.Index}, {entity.Version}) ! TODO: Disconnect from server.");
                }
            }
        }

        private void SendNewInputToServer(NetworkEntity entity, InputPacket packet)
        {
            var msgMgr  = entity.GetNetworkInstance().GetMessageManager();
            var msgData = msgMgr.Create(InputMsgToServerId);

            packet.WriteTo(msgData);

            m_NetworkMessageSystem.InstantSendToAllDefault(entity.GetNetworkInstance(), msgData, DeliveryMethod.ReliableUnordered);
        }

        private void SendNewInputToClients(NetworkInstance caller, InputPacket packet)
        {
            var msgMgr  = caller.GetMessageManager();
            var msgData = msgMgr.Create(InputMsgToClientsId);
            packet.WriteTo(msgData);

            m_NetworkMessageSystem.InstantSendToAllDefault(caller, msgData, DeliveryMethod.ReliableUnordered);
        }
    }

    public struct InputPacket
    {
        public float  Timestamp;
        public Entity Entity;
        public float2 Direction;

        public InputPacket(Entity entity, float2 direction)
        {
            Timestamp = Time.time;
            Entity    = entity;
            Direction = direction;
        }

        public InputPacket(NetDataReader reader)
        {
            Timestamp = reader.GetFloat();
            Entity    = reader.GetEntity();
            Direction = reader.GetVec2();
        }

        public void WriteTo(NetDataWriter writer)
        {
            writer.Put(Timestamp);
            writer.Put(Entity);
            writer.Put(Direction);
        }
    }

    public class DefStGroundRunManageInputClient
    {
        public DefStGroundRunInput CurrentInput;
        public InputAction   MoveAction;

        public void CreateActionMap()
        {
            MoveAction = new InputAction("move", expectedControlLayout: "Stick");
            MoveAction.AppendCompositeBinding("Dpad")
                      .With("Left", "<Keyboard>/a")
                      .With("Right", "<Keyboard>/d")
                      .With("Up", "<Keyboard>/w")
                      .With("Down", "<Keyboard>/s");
        }

        public void Enable()
        {
            MoveAction.Enable();
            MoveAction.performed += MoveActionOnPerformed;
        }

        public void Disable()
        {
            MoveAction.Disable();
            MoveAction.performed -= MoveActionOnPerformed;
        }

        private void MoveActionOnPerformed(InputAction.CallbackContext context)
        {
            CurrentInput = new DefStGroundRunInput(context.ReadValue<Vector2>());
        }
    }
}