using LiteNetLib;
using LiteNetLib.Utils;
using package.stormium.core;
using package.stormiumteam.networking;
using package.stormiumteam.networking.ecs;
using package.stormiumteam.networking.plugins;
using package.stormiumteam.shared;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.Input;

namespace package.stormium.def.Movements.Systems
{
    public class DefStEntityAimManageInputSystem : GameComponentSystem,
                                                   EventReceiveData.IEv
    {
        private static readonly MessageIdent InputMsgToServerId;
        private static readonly MessageIdent InputMsgToClientsId;

        struct NetworkGroup
        {
            public ComponentDataArray<StCharacter>               Characters;
            public ComponentDataArray<NetworkEntity>             NetworkEntities;
            public ComponentDataArray<DefStEntityAimInput>       Inputs;
            public ComponentDataArray<DefStEntityAimClientInput> ClientInputs;

            public EntityArray Entities;

            public readonly int Length;
        }
        
        struct LocalGroup
        {
            public ComponentDataArray<StCharacter>               Characters;
            public SubtractiveComponent<NetworkEntity>             NetworkEntities;
            public ComponentDataArray<DefStEntityAimInput>       Inputs;
            public ComponentDataArray<DefStEntityAimClientInput> ClientInputs;

            public EntityArray Entities;

            public readonly int Length;
        }

        [Inject] private NetworkGroup m_NetworkGroup;
        [Inject] private LocalGroup m_LocalGroup;

        [Inject] private NetworkMessageSystem m_NetworkMessageSystem;

        private DefStEntityAimManageInputClient m_InputClient;

        public float Sensivity = 3f;
        
        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_InputClient = new DefStEntityAimManageInputClient();
            m_InputClient.CreateActionMap();
            m_InputClient.Enable();
        }
 
        protected override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                Sensivity++;
            }

            if (Input.GetKeyDown(KeyCode.KeypadMinus))
            {
                Sensivity--;
            }

            if (Input.GetKeyDown(KeyCode.KeypadMultiply))
            {
                Sensivity = 3f;
            }
            
            var currentInput = m_InputClient.CurrentInput;
            for (int i = 0; i != m_NetworkGroup.Length; i++)
            {
                var netEntity = m_NetworkGroup.NetworkEntities[i];

                InputPacket packet;
                if (GameServerManagement.IsCurrentlyHosting)
                {
                    packet = new InputPacket(netEntity.ToEntity(), m_NetworkGroup.Inputs[i].Aim);

                    SendNewInputToClients(netEntity.GetNetworkInstance(), packet);   
                }
                else
                {
                    if (!m_NetworkGroup.Entities[i].HasComponent<ClientDriveData<DefStEntityAimInput>>())
                        continue;
                    
                    packet = new InputPacket(netEntity.ToEntity(), GetNewRotation(currentInput, m_NetworkGroup.ClientInputs[i].Aim));
                    SendNewInputToServer(netEntity, packet);

                    SetClientInput(m_NetworkGroup.Entities[i], new DefStEntityAimClientInput(packet.Aim));
                }
            }

            for (int i = 0; i != m_LocalGroup.Length; i++)
            {
                if (!m_LocalGroup.Entities[i].HasComponent<ClientDriveData<DefStEntityAimInput>>())
                    continue;

                var newRotation = GetNewRotation(currentInput, m_LocalGroup.ClientInputs[i].Aim);

                EntityUpdateInput(m_LocalGroup.Entities[i], new DefStEntityAimInput(newRotation), false);
                SetClientInput(m_LocalGroup.Entities[i], new DefStEntityAimClientInput(newRotation));
            }
            m_InputClient.ToZero();
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

                EntityUpdateInput(entity, new DefStEntityAimInput(inputPacket.Aim), false);
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

                EntityUpdateInput(entity, new DefStEntityAimInput(inputPacket.Aim), false);
            }
        }

        private void SendNewInputToServer(NetworkEntity entity, InputPacket packet)
        {
            var msgMgr  = GameServerManagement.Main.LocalInstance.GetMessageManager();
            var msgData = msgMgr.Create(InputMsgToServerId);

            packet.WriteTo(msgData);

            m_NetworkMessageSystem.InstantSendToAllDefault(entity.GetNetworkInstance(), msgData, DeliveryMethod.Unreliable);
        }

        private void SendNewInputToClients(NetworkInstance caller, InputPacket packet)
        {
            var msgMgr  = caller.GetMessageManager();
            var msgData = msgMgr.Create(InputMsgToClientsId);
            packet.WriteTo(msgData);

            m_NetworkMessageSystem.InstantSendToAllDefault(caller, msgData, DeliveryMethod.Unreliable);
        }

        // TODO
        // ReSharper disable RedundantAssignment
        private Vector2 GetNewRotation(Vector2 currentInput, Vector2 previous)
        {
            currentInput = new Vector2(-Input.GetAxisRaw("Mouse Y"), Input.GetAxisRaw("Mouse X")) * Sensivity;
                    
            var newRotation = previous + currentInput;
            newRotation.x = Mathf.Clamp(newRotation.x, -89f, 89f);
            newRotation.y = newRotation.y % 360;

            return newRotation;
        }
        // ReSharper restore RedundantAssignment

        private void SetClientInput(Entity entity, DefStEntityAimClientInput input)
        {
            PostUpdateCommands.SetComponent(entity, input);
        }

        private void EntityUpdateInput(Entity entity, DefStEntityAimInput input, bool isServer)
        {
            if (entity.HasComponent<DefStEntityAimInput>())
            {
                entity.SetComponentData(input);
            }
            else
            {
                //TODO: Disconnect from server
                Debug.Log(isServer 
                    ? $"Invalid component for ({entity.Index}, {entity.Version}) ! TODO: Disconnect Player."
                    : $"Invalid component for ({entity.Index}, {entity.Version}) ! TODO: Disconnect from server.");
            }
        }

        public struct InputPacket
        {
            public Entity  Entity;
            public Vector2 Aim;

            public InputPacket(Entity entity, Vector2 aim)
            {
                Entity = entity;
                Aim    = aim;
            }

            public InputPacket(NetDataReader reader)
            {
                Entity = reader.GetEntity();
                var x = reader.GetFloat();
                var y = reader.GetFloat();
                Aim = new Vector2(x, y);
            }

            public void WriteTo(NetDataWriter writer)
            {
                writer.Put(Entity);
                writer.Put(Aim.x);
                writer.Put(Aim.y);
            }
        }
    }

    public class DefStEntityAimManageInputClient
    {
        public Vector2 CurrentInput;
        public InputAction   MoveAction;

        private int m_LastFrame;

        public void CreateActionMap()
        {            
            MoveAction = new InputAction("look", "<Mouse>/delta");
            /*MoveAction.AppendCompositeBinding("Dpad")
                      .With("Left", "<Mouse>/a")
                      .With("Right", "<Keyboard>/d")
                      .With("Up", "<Keyboard>/w")
                      .With("Down", "<Keyboard>/s");*/
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

        /// <summary>
        /// Reset the input to zero
        /// </summary>
        public void ToZero()
        {
            // We actually need to do that as MoveAction is performed multiple time (which is good tbh, as there can be multiple inputs per frame)
            CurrentInput = Vector2.zero;
        }
        
        private void MoveActionOnPerformed(InputAction.CallbackContext context)
        {
            var ctxInput = context.ReadValue<Vector2>();
            CurrentInput += new Vector2(-ctxInput.y, ctxInput.x);
        }
    }
}