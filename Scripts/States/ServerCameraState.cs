using package.stormiumteam.networking.runtime.lowlevel;
using Stormium.Core;
using StormiumShared.Core.Networking;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Stormium.Default.States
{
    public struct ServerCameraState : IStateData, IComponentData
    {
        public struct WritePayload : IWriteEntityDataPayload
        {
            public ComponentDataFromEntity<ServerCameraState> States;
            
            public void Write(int index, Entity entity, DataBufferWriter data, SnapshotReceiver receiver, StSnapshotRuntime runtime)
            {
                var state = States[entity];
            
                data.WriteRef(ref state.Mode);
                data.WriteRef(ref state.Target);
                data.WriteValue((half3) state.PosOffset);
                data.WriteValue((half4) state.RotOffset.value);
            }
        }

        public struct ReadPayload : IReadEntityDataPayload
        {
            public EntityManager EntityManager;
            
            public void Read(int index, Entity entity, ref DataBufferReader data, SnapshotSender sender, StSnapshotRuntime runtime)
            {
                ServerCameraState state = default;

                state.Mode      = data.ReadValue<CameraMode>();
                state.Target    = runtime.EntityToWorld(data.ReadValue<Entity>());
                state.PosOffset = data.ReadValue<half3>();
                state.RotOffset = (float4) data.ReadValue<half4>();

                EntityManager.SetComponentData(entity, state);
            }
        }
        
        public class Streamer : SnapshotEntityDataManualStreamer<ServerCameraState, WritePayload, ReadPayload>
        {
            protected override void UpdatePayloadW(ref WritePayload current)
            {
                current.States = States;
            }

            protected override void UpdatePayloadR(ref ReadPayload current)
            {
                current.EntityManager = EntityManager;
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