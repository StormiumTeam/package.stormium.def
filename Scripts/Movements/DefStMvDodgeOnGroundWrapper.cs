using System;
using package.stormiumteam.shared;
using Unity.Entities;
using UnityEngine;

namespace package.stormium.def
{
    public struct DefStMvDodgeOnGroundExecutable : IComponentData, IExecutableTag
    {
        
    }
    
    [Serializable]
    public struct DefStMvDodgeOnGround : IComponentData
    {
        public float StaminaUse;
        public float VerticalBump;
        public float Cooldown;
        public float MaximalSpeed;
        public Vector3 Direction;
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