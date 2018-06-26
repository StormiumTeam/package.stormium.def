using System;
using package.guerro.shared;
using Unity.Entities;

namespace package.stormium.def
{
    [Serializable]
    public struct DefStMvDodge : IComponentData
    {
        public float MinimumSpeed;
        public float AdditiveForce;
    }

    public class DefStMvDodgeWrapper : BetterComponentWrapper<DefStMvDodge>
    {
        public DefStMvDodgeWrapper()
        {
            Value = new DefStMvDodge
            {
                MinimumSpeed = 10f,
                AdditiveForce = 5f,
            };
        }
    }
}