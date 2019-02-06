using package.stormiumteam.networking.runtime.lowlevel;
using StormiumShared.Core.Networking;
using Unity.Entities;

namespace Stormium.Default.States
{
    public struct OwnerState : IStateData, IComponentData
    {
        public class Streamer : SnapshotEntityDataManualStreamer<OwnerState>
        {
            protected override void WriteDataForEntity(int index, Entity entity, ref DataBufferWriter data, SnapshotReceiver receiver, StSnapshotRuntime runtime)
            {
                var state = EntityManager.GetComponentData<OwnerState>(entity);

                data.WriteRef(ref state.Target);
            }

            protected override void ReadDataForEntity(int index, Entity entity, ref DataBufferReader data, SnapshotSender sender, StSnapshotRuntime runtime)
            {
                var worldTarget = runtime.EntityToWorld(data.ReadValue<Entity>());

                EntityManager.SetComponentData(entity, new OwnerState {Target = worldTarget});
            }
        }

        public Entity Target;
    }
}