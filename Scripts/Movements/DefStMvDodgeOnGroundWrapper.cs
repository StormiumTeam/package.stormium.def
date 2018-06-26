using System;
using package.guerro.shared;
using Unity.Entities;

namespace package.stormium.def
{
    [Serializable]
    public struct DefStMvDodgeOnGround : IComponentData
    {
        public float StaminaUse;
        public float VerticalBump;
        public float Cooldown;
        public float MaximalSpeed;
        public int AirDodgeState;
    }

    public class DefStMvDodgeOnGroundWrapper : BetterComponentWrapper<DefStMvDodgeOnGround>
    {
        public DefStMvDodgeOnGroundWrapper()
        {
            Value = new DefStMvDodgeOnGround
            {
                StaminaUse = 0.25f,
                VerticalBump = 1.25f,
            };
        }
    }
}