using Unity.Entities;
using UnityEngine;

namespace package.stormium.def.characters
{
    public struct StormiumCharacterMvData : IComponentData
    {
        public float BaseSpeed;
        public float Acceleration;
        public float Deacceleration;
        public float AirControl;
        public float AirControlGroundStart;
        public float DodgeAddSpeed;
        public float DodgeMinSpeed;
        public float DodgeMaxSpeed;
        public float DodgeVerticalPower;
        public float JumpVerticalPower;
        public float WallJumpVerticalPower;
        public float WallJumpPower;
        public Vector3 Gravity;
        public float GravityScale;

        public static StormiumCharacterMvData NewBase()
        {
            return new StormiumCharacterMvData
            {
                BaseSpeed             = 9f,
                Acceleration          = 25f,
                Deacceleration        = 10f,
                AirControl            = 8.25f,
                AirControlGroundStart = 22.5f,
                DodgeAddSpeed         = 1f,
                DodgeMinSpeed         = 14,
                DodgeMaxSpeed         = 20,
                DodgeVerticalPower    = 3f,
                JumpVerticalPower     = 5f,
                WallJumpVerticalPower = 6f,
                WallJumpPower         = 6.5f,
                Gravity               = new Vector3(0, -20, 0),
                GravityScale          = 1f
            };
        }
    }

    public struct StormiumCharacterMvProcessData : IComponentData
    {
        public float AirControlScale;
        public Vector3 PrevVelocity;
        public byte PrevGroundFlags;
    }
}