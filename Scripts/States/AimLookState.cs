using StormiumShared.Core.Networking;
using Unity.Entities;
using Unity.Mathematics;

namespace Stormium.Default.States
{
    public struct AimLookState : IStateData, IComponentData
    {
        public float2 Aim;

        public AimLookState(float2 aim)
        {
            Aim = aim;
        }
    }

    public class AimLookStateStreamerBase : SnapshotEntityDataAutomaticStreamer<AimLookState>
    {
        
    }
}