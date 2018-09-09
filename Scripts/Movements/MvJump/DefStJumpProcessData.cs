using Unity.Entities;

namespace package.stormium.def.Movements.Data
{
    public struct DefStJumpProcessData : IComponentData
    {
        public int ComboCtx;
        public float CooldownBeforeNextJump;

        public DefStJumpProcessData(int comboCtx, float cooldownBeforeNextJump)
        {
            ComboCtx = comboCtx;
            CooldownBeforeNextJump = cooldownBeforeNextJump;
        }
    }
}