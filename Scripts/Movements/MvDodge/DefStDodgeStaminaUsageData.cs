using package.stormium.def.Movements;
using Unity.Entities;

namespace Scripts.Movements.MvJump
{
    public struct DefStDodgeStaminaUsageData : IComponentData
    {
        public EStaminaUsage Usage;
        public float BaseRemove;
        public float Needed;
    }
}