using Unity.Entities;

namespace package.stormium.def.Movements.Data
{
    public struct DefStJumpProcessData : IComponentData
    {
        public int ComboCtx;
        public float CooldownBeforeNextJump;
        public byte NeedToChain;

        public DefStJumpProcessData(int comboCtx, float cooldownBeforeNextJump, byte needToChain)
        {
            ComboCtx = comboCtx;
            CooldownBeforeNextJump = cooldownBeforeNextJump;
            NeedToChain = needToChain;
        }
    }
}