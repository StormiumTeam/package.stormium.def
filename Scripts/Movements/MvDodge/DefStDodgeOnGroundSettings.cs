using Unity.Entities;
using UnityEngine;

namespace package.stormium.def.Movements.Data
{
    public struct DefStDodgeOnGroundSettings : IComponentData
    {
        public float VerticalPower;
        public float MaxSpeed;
        public float MinSpeed;
        public float AdditiveSpeed;
        
        [Header("Gravity Settings")]
        public GravityType GravityGravityType;
        public Vector3 Gravity;

        public static DefStDodgeOnGroundSettings NewBase()
        {
            return new DefStDodgeOnGroundSettings()
            {
                VerticalPower = 3f,
                AdditiveSpeed = 1f,
                MinSpeed      = 16.25f,
                MaxSpeed      = 20f,
            };
        }
    }
}