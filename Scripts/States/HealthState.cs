using StormiumShared.Core.Networking;
using Unity.Entities;

namespace Stormium.Default.States
{
    public struct HealthState : IStateData, IComponentData
    {
        public int Health;
        public int MaxHealth;

        public HealthState(int health, int maxHealth)
        {
            Health = health;
            MaxHealth = maxHealth;
        }
    }

    public class HealthStateStreamerBase : SnapshotEntityDataAutomaticStreamer<HealthState>
    {
        
    }
}