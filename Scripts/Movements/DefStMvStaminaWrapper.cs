using System;
using package.guerro.shared;
using Unity.Entities;

namespace package.stormium.def
{
    [Serializable]
    public struct DefStMvStamina : IComponentData
    {
        public float Value;
        public float Max;
        public float GainPerSecond;
    }

    public class DefStMvStaminaWrapper : BetterComponentWrapper<DefStMvStamina>
    {
        public DefStMvStaminaWrapper()
        {
            Value = new DefStMvStamina()
            {
                Value         = 0,
                Max           = 1.25f,
                GainPerSecond = 0.25f
            };
        }
    }
}