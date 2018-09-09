using System;
using package.stormiumteam.networking.plugins;
using LiteNetLib;
using LiteNetLib.Utils;
using package.stormium.core;
using package.stormium.def.Movements.Data;
using package.stormium.def.Network;
using package.stormiumteam.networking;
using package.stormiumteam.networking.ecs;
using package.stormiumteam.networking.plugins;
using package.stormiumteam.shared;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Input;

namespace package.stormium.def.Movements.Systems
{
    public class DefStRunManageInputSystem : GameComponentSystem,
                                                   EventReceiveData.IEv
    {
        private static readonly MessageIdent InputMsgToServerId;
        private static readonly MessageIdent InputMsgToClientsId;

        struct NetworkGroup
        {
            public ComponentDataArray<StCharacter>   Characters;
            public ComponentDataArray<NetworkEntity> NetworkEntities;
            public ComponentDataArray<DefStRunInput> Inputs;
            public ComponentDataArray<DefStRunClientInput> ClientInputs;
            public EntityArray Entities;

            public readonly int Length;
        }
        
        struct LocalGroup
        {
            public ComponentDataArray<StCharacter>         Characters;
            public SubtractiveComponent<NetworkEntity>       NetworkEntities;
            public ComponentDataArray<DefStRunInput>       Inputs;
            public ComponentDataArray<DefStRunClientInput> ClientInputs;
            public EntityArray                             Entities;

            public readonly int Length;
        }


        [Inject] private NetworkGroup m_NetworkGroup;
        [Inject] private LocalGroup m_LocalGroup;

        [Inject] private NetworkMessageSystem m_NetworkMessageSystem;

        private DefStRunManageInputClient m_InputClient;

        protected override void OnCreateManager(int capacity)
        {
            base.OnCreateManager(capacity);

            m_InputClient = new DefStRunManageInputClient();
            m_InputClient.CreateActionMap();
            m_InputClient.Enable();
        }
 
        protected override void OnUpdate()
        {
            var currentInput = m_InputClient.CurrentInput;
            for (var i = 0; i != m_NetworkGroup.Length; i++)
            {
                var netEntity = m_NetworkGroup.NetworkEntities[i];

                InputPacket packet;
                if (GameServerManagement.IsCurrentlyHosting)
                {
                    packet = new InputPacket(netEntity.ToEntity(), m_NetworkGroup.Inputs[i].Direction)
                    {
                        Timestamp = m_NetworkGroup.Inputs[i].Timestamp
                    };

                    SendNewInputToClients(netEntity.GetNetworkInstance(), packet);   
                }
                else
                {
                    if (!m_NetworkGroup.Entities[i].HasComponent<ClientDriveData<DefStRunInput>>())
                        continue;
                        
                    packet = new InputPacket(netEntity.ToEntity(), currentInput.Direction);

                    SendNewInputToServer(netEntity, packet);
                    SetClientInput(m_NetworkGroup.Entities[i], new DefStRunClientInput(packet.Direction));
                }
            }

            for (var i = 0; i != m_LocalGroup.Length; i++)
            {
                EntityUpdateInput(m_LocalGroup.Entities[i], new DefStRunInput(currentInput.Direction), false);
                SetClientInput(m_LocalGroup.Entities[i], new DefStRunClientInput(currentInput.Direction));
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

                if (!EntityManager.Exists(entity))
                {
                    //TODO: Disconnect player
                    Debug.Log($"Invalid entity ({entity.Index}, {entity.Version}) ! TODO: Disconnect Player");
                    return;
                }

                EntityUpdateInput(entity, new DefStRunInput(inputPacket.Timestamp, inputPacket.Direction), true);
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

                EntityUpdateInput(entity, new DefStRunInput(inputPacket.Timestamp, inputPacket.Direction), false);
            }
        }

        private void SendNewInputToServer(NetworkEntity entity, InputPacket packet)
        {
            if (!IsConnectedOrHosting) return;
            
            var msgMgr  = GameServerManagement.Main.LocalInstance.GetMessageManager();
            var msgData = msgMgr.Create(InputMsgToServerId);

            packet.WriteTo(msgData);

            m_NetworkMessageSystem.InstantSendToAllDefault(entity.GetNetworkInstance(), msgData, DeliveryMethod.ReliableUnordered);
        }

        private void SendNewInputToClients(NetworkInstance caller, InputPacket packet)
        {
            if (!IsConnectedOrHosting) return;
            
            var msgMgr  = caller.GetMessageManager();
            var msgData = msgMgr.Create(InputMsgToClientsId);
            packet.WriteTo(msgData);

            m_NetworkMessageSystem.InstantSendToAllDefault(caller, msgData, DeliveryMethod.ReliableUnordered);
        }

        private void SetClientInput(Entity entity, DefStRunClientInput input)
        {
            PostUpdateCommands.SetComponent(entity, input);
        }

        private void EntityUpdateInput(Entity entity, DefStRunInput input, bool isServer)
        {
            if (entity.HasComponent<DefStRunInput>())
            {
                entity.SetComponentData(input);
            }
            else
            {
                //TODO: Disconnect from server
                Debug.Log(!isServer
                    ? $"Invalid component for ({entity.Index}, {entity.Version}) ! TODO: Disconnect from server."
                    : $"Invalid component for ({entity.Index}, {entity.Version}) ! TODO: Disconnect from client.");
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
    }

    public class DefStRunManageInputClient
    {
        public DefStRunInput CurrentInput;
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
            CurrentInput = new DefStRunInput(context.ReadValue<Vector2>());
        }
    }
}