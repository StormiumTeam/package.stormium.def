using Unity.Entities;

namespace package.stormium.def
{
    public struct StStamina : IComponentData
    {
        public float Value;
        public float Max;
        public float Gain;
    }
}