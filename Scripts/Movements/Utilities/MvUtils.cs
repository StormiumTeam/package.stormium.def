using package.stormiumteam.shared;
using UnityEngine;

namespace package.stormium.def.Utilities
{
    public static class MvUtils
    {
        public static bool OnGround(CharacterControllerMotor controllerMotor, StVelocity velocity)
        {
            return velocity.Value.y <= 0 && controllerMotor.IsGrounded(CPhysicSettings.PhysicInteractionLayerMask);
        }
        
        public static bool OnGround(CharacterControllerMotor controllerMotor, Vector3 velocity)
        {
            return velocity.y <= 0 && controllerMotor.IsGrounded(CPhysicSettings.PhysicInteractionLayerMask);
        }
        
        public static bool OnGround(CharacterControllerState state, StVelocity velocity)
        {
            return velocity.Value.y <= 0 && state.GroundFlags == 1;
        }
        
        public static bool OnGround(CharacterControllerState state, Vector3 velocity)
        {
            return velocity.y <= 0 && state.GroundFlags == 1;
        }
    }
}