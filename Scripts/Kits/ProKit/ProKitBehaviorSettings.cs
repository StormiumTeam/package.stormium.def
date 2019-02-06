using Unity.Entities;

namespace package.stormium.def.Kits.ProKit
{
    public struct ProKitBehaviorSettings : IComponentData
    {
        public SrtGroundSettings GroundSettings;
        public SrtAerialSettings AerialSettings;

        public float AirTime;
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