using System;
using package.stormiumteam.shared;
using Unity.Entities;

namespace package.stormium.def
{
    public struct DefStMvDodgeOnWallExecutable : IComponentData, IExecutableTag
    {
        
    }
    
    [Serializable]
    public struct DefStMvDodgeOnWall : IComponentData
    {
        public float StaminaUse;
        public float VerticalBump;
        public float Cooldown;
        public float MaximalSpeed;
    }

    public class DefStMvDodgeOnWallWrapper : BetterComponentWrapper<DefStMvDodgeOnWall>
    {
        public DefStMvDodgeOnWallWrapper()
        {
            Value = new DefStMvDodgeOnWall
            {
                StaminaUse = 0.25f,
                VerticalBump = 0.1f,
                MaximalSpeed = 15f
            };
        }
    }
}