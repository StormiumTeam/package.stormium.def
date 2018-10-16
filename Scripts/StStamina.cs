using Unity.Entities;

namespace package.stormium.def
{
    public struct StStamina : IComponentData
    {
        public float Value;
        public float Max;
        public float Gain;

        public StStamina(float max, float gain, float value = 1f)
        {
            Value = value;
            Max = max;
            Gain = gain;
        }
    }
}