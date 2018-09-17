using Unity.Entities;
using UnityEngine;

namespace package.stormium.def.Movements.Data
{
    public struct DefStJumpSettings : IComponentData
    {
        public float JumpPower;
        public int MaxCombo;
        
        [Header("Gravity Settings")]
        public GravityType GravityGravityType;
        public Vector3 Gravity;
        
        public static DefStJumpSettings NewBase()
        {
            return new DefStJumpSettings()
            {
                JumpPower = 0.375f,
                MaxCombo  = 1
            };
        }
    }
}