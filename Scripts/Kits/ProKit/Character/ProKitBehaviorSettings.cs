using StormiumShared.Core.Networking;
using Unity.Entities;

namespace package.stormium.def.Kits.ProKit
{
    public struct ProKitMovementSettings : IComponentData
    {
        public SrtGroundSettings GroundSettings;
        public SrtAerialSettings AerialSettings;
    }

    public struct ProKitMovementState : IStateData, IComponentData
    {
        public int   AirTime;
        public byte  ForceUnground;
        public long  WallBounceTick;
        public float AirControl;
    }

    public struct AirTime : IComponentData
    {
        public float Value;

        public AirTime(float value)
        {
            Value = value;
        }
    }
}