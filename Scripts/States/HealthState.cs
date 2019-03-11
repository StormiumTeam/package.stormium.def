using package.stormiumteam.networking.runtime.lowlevel;
using StormiumShared.Core.Networking;
using Unity.Entities;

namespace Stormium.Default.States
{
    public struct HealthState : IStateData, IComponentData, ISerializableAsPayload
    {
        public int Health;
        public int MaxHealth;

        public HealthState(int health, int maxHealth)
        {
            Health    = health;
            MaxHealth = maxHealth;
        }

        public void Write(ref DataBufferWriter data, SnapshotReceiver receiver, SnapshotRuntime runtime)
        {
            data.WriteDynamicIntWithMask((ulong) Health, (ulong) MaxHealth);
        }

        public void Read(ref DataBufferReader data, SnapshotSender sender, SnapshotRuntime runtime)
        {
            data.ReadDynIntegerFromMask(out var r1, out var r2);

            Health    = (int) r1;
            MaxHealth = (int) r2;
        }

        public class Streamer : SnapshotEntityDataManualValueTypeStreamer<HealthState>
        {
        }
    }
}