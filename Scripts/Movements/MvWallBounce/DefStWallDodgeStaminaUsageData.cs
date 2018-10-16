using package.stormium.def.Movements;
using Unity.Entities;

namespace Scripts.Movements.MvWallBounce
{
    public struct DefStWallDodgeStaminaUsageData : IComponentData
    {
        public EStaminaUsage Usage;
        public float Needed;
        public float Remove;
    }
}