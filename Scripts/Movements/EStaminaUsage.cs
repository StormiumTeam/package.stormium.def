using System;

namespace package.stormium.def.Movements
{
    [Flags]
    public enum EStaminaUsage
    {
        NoUsage = 0,
        RemoveStamina = 1 << 0,
        BlockAction = 2 << 0
    }
}