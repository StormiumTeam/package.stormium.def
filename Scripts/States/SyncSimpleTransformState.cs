using package.stormiumteam.networking.runtime.lowlevel;
using StormiumShared.Core.Networking;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Mathematics;

namespace Stormium.Default.States
{
    public class SyncSimpleTransformState : SnapshotEntityDataManualStreamer<TransformState, SyncSimpleTransformState.WritePayload, SyncSimpleTransformState.ReadPayload>
    {
        public struct WritePayload : IWriteEntityDataPayload<TransformState>
        {
            public void Write(int index, Entity entity, ComponentDataFromEntity<TransformState> stateFromEntity, ComponentDataFromEntity<DataChanged<TransformState>> changeFromEntity, DataBufferWriter data, SnapshotReceiver receiver, SnapshotRuntime runtime)
            {                
                var state = stateFromEntity[entity];

                data.WriteUnmanaged((half3) state.Position);
                data.WriteUnmanaged((half4) state.Rotation.value);
            }
        }
        
        public struct ReadPayload : IReadEntityDataPayload<TransformState>
        {
            public EntityManager EntityManager;
            
            public void Read(int index, Entity entity, ComponentDataFromEntity<TransformState> dataFromEntity, ref DataBufferReader data, SnapshotSender sender, SnapshotRuntime runtime)
            {
                TransformState state;

                state.Position = data.ReadValue<half3>();
                state.Rotation = new quaternion(data.ReadValue<half4>());

                if (EntityManager.HasComponent<InterpolationBuffer>(entity))
                {
                    var buffer = EntityManager.GetBuffer<InterpolationBuffer>(entity);
                    buffer.Add(new InterpolationBuffer(state, runtime.Header.SnapshotIdx, runtime.Header.GameTime.Tick));
                
                    return;
                }

                dataFromEntity[entity] = state;
            }
        }

        protected override void UpdatePayloadW(ref WritePayload current)
        {
        }
        
        protected override void UpdatePayloadR(ref ReadPayload current)
        {
            current.EntityManager = EntityManager;
        }
    }
}