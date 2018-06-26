using System;
using package.guerro.shared;
using Unity.Entities;

namespace package.stormium.def
{
    [Serializable]
    public struct DefStMvWalljump : IComponentData
    {
        public float StaminaUse;
        public float WallForce;
        public float DirectionForce;
        public float VerticalBump;
        public float Cooldown;
        public float MaximalSpeed;
        public float MinimumSpeed;
    }

    public class DefStMvWalljumpWrapper : BetterComponentWrapper<DefStMvWalljump>
    {
        public DefStMvWalljumpWrapper()
        {
            Value = new DefStMvWalljump
            {
                StaminaUse = 0.1f,
                WallForce = 8f,
                DirectionForce = 4f,
                VerticalBump = 0.35f,
                MaximalSpeed = 25f,
                MinimumSpeed = 5f
            };
        }
    }
}