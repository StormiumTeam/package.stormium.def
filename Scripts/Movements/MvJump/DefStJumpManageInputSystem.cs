using package.stormiumteam.networking.plugins;
using LiteNetLib;
using package.stormium.core;
using package.stormium.def.Movements.Data;
using package.stormiumteam.networking;
using package.stormiumteam.networking.ecs;
using package.stormiumteam.shared;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.Input;

namespace package.stormium.def.Movements.Systems
{
    public enum DefStJumpState
    {
        Once,
        ReplayOnGround
    }

    [AlwaysUpdateSystem]
    public class DefStJumpManageInputSystem : GameComponentSystem,
                                              EventReceiveData.IEv
    {
        private static readonly MessageIdent SendDoJumpMsgId;

        struct NetworkGroup
        {
            public ComponentDataArray<StCharacter>    Characters;
            public ComponentDataArray<NetworkEntity>  NetworkEntities;
            public ComponentDataArray<DefStJumpInput> Inputs;
            public EntityArray Entities;

            public readonly int Length;
        }
        
        struct LocalGroup
        {
            public ComponentDataArray<StCharacter>    Characters;
            public SubtractiveComponent<NetworkEntity> NetworkEntities;
            public ComponentDataArray<DefStJumpInput> Inputs;
            public EntityArray                        Entities;

            public readonly int Length;
        }

        [Inject] private NetworkGroup m_NetworkGroup;
        [Inject] private LocalGroup m_LocalGroup;

        [Inject] private NetworkMessageSystem m_NetworkMessageSystem;

        private DefStJumpManageInputClient m_InputClient;

        protected override void OnCreateManager(int capacity)
        {
            Debug.Log("<color='red'>" + GetType().Name + "</color>");
            
            base.OnCreateManager(capacity);

            m_InputClient = new DefStJumpManageInputClient();
            m_InputClient.CreateActionMap();
            m_InputClient.Enable();
        }

        protected override void OnUpdate()
        {
            var shouldJump = m_InputClient.LastFrameInput == Time.frameCount;
            for (int i = 0; i != m_NetworkGroup.Length; i++)
            {
                var netEntity = m_NetworkGroup.NetworkEntities[i];

                if (GameServerManagement.IsCurrentlyHosting) continue;
                if (!m_NetworkGroup.Entities[i].HasComponent<ClientDriveData<DefStJumpInput>>())
                    continue;

                if (shouldJump)
                {
                    SendDoJumpToServer(netEntity);
                }

                m_NetworkGroup.Inputs[i] = new DefStJumpInput(shouldJump ? 1 : 0, 0.1f);
            }
            for (int i = 0; i != m_LocalGroup.Length; i++)
            {
                if (!m_LocalGroup.Entities[i].HasComponent<ClientDriveData<DefStJumpInput>>())
                    continue;

                if (shouldJump)
                {
                    EntityJump(m_LocalGroup.Entities[i]);
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
            var conEntityMgr  = args.PeerInstance.Get<ConnectionEntityManager>();
            var msgPattern    = conPatternMgr.GetPattern(args.Reader);
            if (msgPattern == SendDoJumpMsgId)
            {
                var entity = args.Reader.GetEntity();

                if (!EntityManager.Exists(entity))
                {
                    //TODO: Disconnect player
                    Debug.Log($"Invalid entity ({entity.Index}, {entity.Version}) ! TODO: Disconnect Player");
                    return;
                }
                
                EntityJump(entity);
            }
        }

        private void EntityJump(Entity entity)
        {
            if (entity.HasComponent<DefStJumpInput>())
            {
                entity.SetComponentData(new DefStJumpInput(InputState.Down, 0.2f));
            }
            else
            {
                //TODO: Disconnect player
                Debug.Log($"Invalid component for ({entity.Index}, {entity.Version}) ! TODO: Disconnect Player");
            }
        }

        private void SendDoJumpToServer(NetworkEntity entity)
        {
            var msgMgr  = GameServerManagement.Main.LocalInstance.GetMessageManager();
            var msgData = msgMgr.Create(SendDoJumpMsgId);
            msgData.Put(entity.ToEntity());

            m_NetworkMessageSystem.InstantSendToAllDefault(entity.GetNetworkInstance(), msgData, DeliveryMethod.ReliableOrdered);
        }
    }

    public class DefStJumpManageInputClient
    {
        public int LastFrameInput;
        public InputAction    MoveAction;

        public void CreateActionMap()
        {
            MoveAction = new InputAction("jump", "<Keyboard>/space", "press");
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
            var value = context.ReadValue<float>();
            LastFrameInput = Time.frameCount;
        }
    }
}