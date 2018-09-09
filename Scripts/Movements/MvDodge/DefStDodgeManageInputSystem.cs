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
    [AlwaysUpdateSystem]
    public class DefStDodgeManageInputSystem : GameComponentSystem,
                                              EventReceiveData.IEv
    {
        private static readonly MessageIdent SendDoDodgeMsgId;

        struct NetworkGroup
        {
            public ComponentDataArray<StCharacter>    Characters;
            public ComponentDataArray<NetworkEntity>  NetworkEntities;
            public ComponentDataArray<DefStDodgeInput> Inputs;
            public EntityArray Entities;

            public readonly int Length;
        }

        struct LocalGroup
        {
            public ComponentDataArray<StCharacter>     Characters;
            public SubtractiveComponent<NetworkEntity>   NetworkEntities;
            public ComponentDataArray<DefStDodgeInput> Inputs;
            public EntityArray                         Entities;

            public readonly int Length;
        }
        
        [Inject] private NetworkGroup m_NetworkGroup;
        [Inject] private LocalGroup m_LocalGroup;

        [Inject] private NetworkMessageSystem m_NetworkMessageSystem;

        private DefStDodgeManageInputClient m_InputClient;

        protected override void OnCreateManager(int capacity)
        {
            Debug.Log("<color='red'>" + GetType().Name + "</color>");
            
            base.OnCreateManager(capacity);

            m_InputClient = new DefStDodgeManageInputClient();
            m_InputClient.CreateActionMap();
            m_InputClient.Enable();
        }

        protected override void OnUpdate()
        {
            var shouldDodge = m_InputClient.LastFrameInput == Time.frameCount;
            for (int i = 0; i != m_NetworkGroup.Length; i++)
            {
                var netEntity = m_NetworkGroup.NetworkEntities[i];

                if (GameServerManagement.IsCurrentlyHosting) continue;
                if (!m_NetworkGroup.Entities[i].HasComponent<WeOwnThisEntity>())
                    continue;

                if (shouldDodge)
                {
                    SendDoDodgeToServer(netEntity);
                }

                m_NetworkGroup.Inputs[i] = new DefStDodgeInput(shouldDodge ? 1 : 0, 0.1f);
            }

            for (int i = 0; i != m_LocalGroup.Length; i++)
            {
                if (!m_LocalGroup.Entities[i].HasComponent<ClientDriveData<DefStDodgeInput>>())
                    continue;

                if (shouldDodge)
                {
                    EntityDodge(m_LocalGroup.Entities[i]);
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
            if (msgPattern == SendDoDodgeMsgId)
            {
                var entity = args.Reader.GetEntity();

                if (!EntityManager.Exists(entity))
                {
                    //TODO: Disconnect player
                    Debug.Log($"Invalid entity ({entity.Index}, {entity.Version}) ! TODO: Disconnect Player");
                    return;
                }

                EntityDodge(entity);
            }
        }

        private void EntityDodge(Entity entity)
        {
            if (entity.HasComponent<DefStDodgeInput>())
            {
                entity.SetComponentData(new DefStDodgeInput(InputState.Down, 0.1f));
            }
            else
            {
                //TODO: Disconnect player
                Debug.Log($"Invalid component for ({entity.Index}, {entity.Version}) ! TODO: Disconnect Player");
            }
        }

        private void SendDoDodgeToServer(NetworkEntity entity)
        {
            var msgMgr  = GameServerManagement.Main.LocalInstance.GetMessageManager();
            var msgData = msgMgr.Create(SendDoDodgeMsgId);
            msgData.Put(entity.ToEntity());

            m_NetworkMessageSystem.InstantSendToAllDefault(entity.GetNetworkInstance(), msgData, DeliveryMethod.ReliableOrdered);
        }
    }

    public class DefStDodgeManageInputClient
    {
        public int LastFrameInput;
        public InputAction    MoveAction;

        public void CreateActionMap()
        {
            MoveAction = new InputAction("dodge", "<Keyboard>/leftShift", "press");
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