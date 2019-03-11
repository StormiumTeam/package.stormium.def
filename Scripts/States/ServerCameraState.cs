using package.stormiumteam.networking.runtime.lowlevel;
using StormiumShared.Core.Networking;
using StormiumTeam.GameBase;
using Unity.Entities;
using Unity.Mathematics;

namespace Stormium.Default.States
{
    public struct ServerCameraState : IStateData, IComponentData
    {
        public struct WritePayload : IWriteEntityDataPayload<ServerCameraState>
        {
            public void Write(int index, Entity entity, ComponentDataFromEntity<ServerCameraState> stateFromEntity, ComponentDataFromEntity<DataChanged<ServerCameraState>> changeFromEntity, DataBufferWriter data, SnapshotReceiver receiver, SnapshotRuntime runtime)
            {
                var state = stateFromEntity[entity];
            
                data.WriteRef(ref state.Mode);
                data.WriteRef(ref state.Target);
                data.WriteUnmanaged((half3) state.PosOffset);
                data.WriteUnmanaged((half4) state.RotOffset.value);
            }
        }

        public struct ReadPayload : IReadEntityDataPayload<ServerCameraState>
        {
            public void Read(int index, Entity entity, ComponentDataFromEntity<ServerCameraState> dataFromEntity, ref DataBufferReader data, SnapshotSender sender, SnapshotRuntime runtime)
            {
                ServerCameraState state = default;

                state.Mode      = data.ReadValue<CameraMode>();
                state.Target    = runtime.EntityToWorld(data.ReadValue<Entity>());
                state.PosOffset = data.ReadValue<half3>();
                state.RotOffset = (float4) data.ReadValue<half4>();

                dataFromEntity[entity] = state;
            }
        }
        
        public class Streamer : SnapshotEntityDataManualStreamer<ServerCameraState, WritePayload, ReadPayload>
        {
            protected override void UpdatePayloadW(ref WritePayload current)
            {
            }

            protected override void UpdatePayloadR(ref ReadPayload current)
            {
            }
        }
        
        public CameraMode Mode;
        
        public Entity Target;
        public float3 PosOffset;
        public quaternion RotOffset;

        public ServerCameraState(Entity target, float3 posOffset = default, quaternion rotOffset = default)
        {
            if (!math.all(rotOffset.value))
                rotOffset = quaternion.identity;

            Target = target;
            
            PosOffset = posOffset;
            RotOffset = rotOffset;

            Mode = CameraMode.Default;
        }
    }
}