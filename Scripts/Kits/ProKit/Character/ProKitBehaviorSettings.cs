using System;
using Unity.Entities;
using Unity.Mathematics;

namespace package.stormium.def.Kits.ProKit
{
    public struct ProKitMovementSettings : IComponentData
    {
        public SrtGroundSettings GroundSettings;
        public SrtAerialSettings AerialSettings;
    }

    public struct ProKitMovementState : IComponentData
    {
        [Flags]
        public enum ESpecialMovement
        {
            None       = 0,
            Jump       = 1,
            Dodge      = 2,
            WallBounce = 4,
            WallJump   = 5,
            WallDodge  = 6
        }

        public ESpecialMovement LastSpecialMovement;

        public int    AirTime;
        public bool   ForceUnground;
        public long   WallBounceTick;
        public float  AirControl;
        public float3 LastMove;

        public bool   IsSliding;
        public float3 SlideNormal;
        public int    LastWallDodge;
    }

    public struct AirTime : IComponentData
    {
        public float Value;

        public AirTime(float value)
        {
            Value = value;
        }
    }
}