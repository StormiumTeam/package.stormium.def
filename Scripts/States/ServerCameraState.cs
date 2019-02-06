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

    public class ServerCameraStateStreamerBase : SnapshotEntityDataManualStreamer<ServerCameraState>
    {
        protected override void WriteDataForEntity(int index, Entity entity, ref DataBufferWriter data, SnapshotReceiver receiver, StSnapshotRuntime runtime)
        {
            var state = EntityManager.GetComponentData<ServerCameraState>(entity);
            
            data.WriteRef(ref state.Mode);
            data.WriteRef(ref state.Target);
            data.WriteValue((half3) state.PosOffset);
            data.WriteValue((half4) state.RotOffset.value);
        }

        protected override void ReadDataForEntity(int index, Entity entity, ref DataBufferReader data, SnapshotSender sender, StSnapshotRuntime runtime)
        {
            ServerCameraState state = default;

            state.Mode      = data.ReadValue<CameraMode>();
            state.Target    = runtime.EntityToWorld(data.ReadValue<Entity>());
            state.PosOffset = data.ReadValue<half3>();
            state.RotOffset = (float4) data.ReadValue<half4>();

            EntityManager.SetComponentData(entity, state);
        }
    }
}