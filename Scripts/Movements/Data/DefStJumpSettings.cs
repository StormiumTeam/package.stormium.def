using Unity.Entities;
using UnityEngine;

namespace package.stormium.def.Movements.Data
{
    public struct DefStJumpSettings : IComponentData
    {
        public float JumpPower;
        
        [Header("Gravity Settings")]
        public GravityType GravityGravityType;
        public Vector3 Gravity;
    }
}