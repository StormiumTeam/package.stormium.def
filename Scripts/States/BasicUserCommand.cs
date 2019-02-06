using System;
using System.IO;
using package.stormiumteam.networking;
using package.stormiumteam.networking.runtime.highlevel;
using package.stormiumteam.networking.runtime.lowlevel;
using Runtime.Data;
using StormiumShared.Core.Networking;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Experimental.Input;

namespace Stormium.Default.States
{
    public struct BasicUserCommand : IStateData, IComponentData
    {
        public float2 Move;
        public float2 Look;

        public BasicUserCommand(IntPtr ctx)
        {
            Move = float2.zero;
            Look = float2.zero;
        }
    }

    public class BasicUserCommandStreamer : SnapshotEntityDataManualStreamer<BasicUserCommand>
    {
        protected override void WriteDataForEntity(int index, Entity entity, ref DataBufferWriter data, SnapshotReceiver receiver, StSnapshotRuntime runtime)
        {
            data.WriteValue(EntityManager.GetComponentData<BasicUserCommand>(entity));
        }

        protected override void ReadDataForEntity(int index, Entity entity, ref DataBufferReader data, SnapshotSender sender, StSnapshotRuntime runtime)
        {
            var cmd = data.ReadValue<BasicUserCommand>();
            // If the entity is attached to a player (in all cases) and if it's our own player, we don't set the new data.
            if (EntityManager.HasComponent<StGamePlayer>(entity))
            {
                if (EntityManager.GetComponentData<StGamePlayer>(entity).IsSelf == 1)
                    return;
            }
            
            EntityManager.SetComponentData(entity, cmd);
        }
    }

    public class BasicUserCommandUpdateLocal : ComponentSystem
    {
        private InputActionAsset m_Asset;
        private InputActionMap m_InputMap;
        private InputAction m_MoveAction, m_LookAction;
        
        private BasicUserCommand m_ActualCommand;

        private PatternResult m_SyncCommandId;

        protected override void OnCreateManager()
        {
            var file = File.ReadAllText(Application.streamingAssetsPath + "/input.json");

            m_Asset = ScriptableObject.CreateInstance<InputActionAsset>();
            m_Asset.LoadFromJson(file);
            
            Refresh();

            m_SyncCommandId = World.GetOrCreateManager<NetPatternSystem>().GetLocalBank().Register("000SyncBasicUserCommand");
        }

        protected override unsafe void OnUpdate()
        {
            m_ActualCommand.Look = GetNewAimLook(m_ActualCommand.Look, new float2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")));
            
            ForEach((ref StGamePlayer player, ref BasicUserCommand command) =>
            {
                if (player.IsSelf == 0)
                    return;
                
                command = m_ActualCommand;
            });
            
            // Send or receive the inputs
            ForEach((Entity entity, ref NetworkInstanceData data) =>
            {
                var patternSystem = World.GetExistingManager<NetPatternSystem>();
                var networkMgr = World.GetExistingManager<NetworkManager>();
                
                if (data.InstanceType == InstanceType.Server)
                {
                    var buffer = BufferHelper.CreateFromPattern(m_SyncCommandId.Id);
                    buffer.WriteRef(ref m_ActualCommand);
                    
                    data.Commands.Send(buffer, default, Delivery.Unreliable);
                    buffer.Dispose();
                }

                if (!data.IsLocal())
                    return;

                var evBuffer = EntityManager.GetBuffer<EventBuffer>(entity);

                for (var i = 0; i != evBuffer.Length; i++)
                {
                    var ev = evBuffer[i].Event;
                    if (ev.Type != NetworkEventType.DataReceived)
                        continue;

                    var foreignEntity = networkMgr.GetNetworkInstanceEntity(ev.Invoker.Id);
                    var exchange      = patternSystem.GetLocalExchange(ev.Invoker.Id);
                    var buffer        = BufferHelper.ReadEventAndGetPattern(ev, exchange, out var patternId);

                    if (patternId != m_SyncCommandId.Id
                        || !EntityManager.HasComponent<NetworkInstanceToClient>(foreignEntity))
                        continue;

                    var clientEntity = EntityManager.GetComponentData<NetworkInstanceToClient>(foreignEntity).Target;
                    if (!EntityManager.HasComponent<StNetworkClientToGamePlayer>(clientEntity))
                        continue;

                    var userCommand  = buffer.ReadValue<BasicUserCommand>();
                    var playerEntity = EntityManager.GetComponentData<StNetworkClientToGamePlayer>(clientEntity).Target;

                    EntityManager.SetComponentData(playerEntity, userCommand);
                }
            });
        }

        private void Refresh()
        {
            m_InputMap = m_Asset.TryGetActionMap("Map");
            if (m_InputMap == null)
                throw new Exception("InputActionMap 'Map' not found.");

            m_MoveAction = m_InputMap.TryGetAction("Move");
            if (m_MoveAction == null)
                throw new Exception("InputAction 'Move' not found");
            
            m_LookAction = m_InputMap.TryGetAction("Look");
            if (m_MoveAction == null)
                throw new Exception("InputAction 'Look' not found");
            
            m_Asset.Enable();

            m_MoveAction.performed += (cc) =>
            {
                m_ActualCommand.Move = cc.ReadValue<Vector2>();
            };
            m_LookAction.performed += (cc) =>
            {
                // weird af in builds
                //m_ActualCommand.Look = GetNewAimLook(m_ActualCommand.Look, cc.ReadValue<Vector2>());
            };
        } 
        
        private float2 GetNewAimLook(float2 previous, float2 next)
        {
            var input = next * 1.5f;
                    
            var newRotation = previous + input;
            newRotation.x = newRotation.x % 360;
            newRotation.y = Mathf.Clamp(newRotation.y, -89f, 89f);

            return newRotation;
        }
    }
}