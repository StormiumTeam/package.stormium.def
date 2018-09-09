using package.stormium.core;
using package.stormium.def.Movements.Data;
using package.stormiumteam.shared;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace package.stormium.def.Movements.Systems
{
    [UpdateAfter(typeof(DefStRunManageInputSystem))]
    public class DefStGroundRunProcessSystem : ComponentSystem
    {
        struct Group
        {
            public ComponentDataArray<StVelocity> Velocities;
            public ComponentDataArray<DefStGroundRunSettings> Settings;
            public ComponentDataArray<DefStRunInput> Inputs;
            public ComponentArray<CharacterControllerMotor> Motors;
            public SubtractiveComponent<VoidSystem<DefStGroundRunProcessSystem>> Void1;

            public readonly int Length;
        }

        [Inject] private PhysicUpdaterSystem m_PhysicUpdaterSystem;
        [Inject] private Group m_Group;

        protected override void OnUpdate()
        {
            for (int frameIndex = 0; frameIndex != m_PhysicUpdaterSystem.LastIterationCount; frameIndex++)
            {
                SimulatePhysicStep(frameIndex, m_PhysicUpdaterSystem.LastFixedTimeStep);
            }
        }

        private void SimulatePhysicStep(int frameIndex, float dt)
        {
            for (int i = 0; i != m_Group.Length; i++)
            {
                var velocityData = m_Group.Velocities[i];
                var input        = m_Group.Inputs[i];
                var motor        = m_Group.Motors[i];
                var settings     = m_Group.Settings[i];

                if (!motor.IsGrounded() || motor.IsSliding) continue;

                velocityData.Value = SrtFixNaN(velocityData.Value);

                var direction   = SrtComputeDirection(motor.transform.rotation, input.Direction);
                var newVelocity = SrtMove(velocityData.Value, direction, settings, motor.IsStableOnGround, dt);

                newVelocity.y = velocityData.Value.y;

                m_Group.Velocities[i] = new StVelocity((Vector3)newVelocity);
            }
        }
        
        // -------------------------------------------------- //
        // TODO: Jobify this part (the bottom uh)
        // ...
        
        /// <summary>
        /// Compute the direction from a rotation and from a given direction
        /// </summary>
        /// <param name="worldRotation">The character rotation</param>
        /// <param name="inputDirection">The direction input</param>
        /// <returns>The new direction</returns>
        private static float3 SrtComputeDirection(Quaternion worldRotation, float2 inputDirection)
        {
            return math.normalize(worldRotation * new Vector3(inputDirection.x, 0, inputDirection.y));
        }
        
        /// <summary>
        /// Move the character with the Srt (CPMA based) algorithm.
        /// </summary>
        /// <param name="initialVelocity">The initial velocity to use</param>
        /// <param name="direction">The movement direction</param>
        /// <param name="settings">The movement settings</param>
        /// <param name="dt">Delta time</param>
        /// <returns>Return the new position</returns>
        private static float3 SrtMove(float3 initialVelocity, float3 direction, DefStGroundRunSettings settings, bool isStable, float dt)
        {
            // Fix NaN errors
            direction = SrtFixNaN(direction);

            // Set Y axe to zero
            initialVelocity.y = 0;
            
            var currentSpeed = math.length(initialVelocity);
            var friction = SrtGetFrictionPower
            (
                currentSpeed,
                settings.FrictionSpeedMin, settings.FrictionSpeedMax,
                settings.FrictionMin, settings.FrictionMax
            );

            var velocity = SrtApplyFriction(initialVelocity, friction, settings.SurfaceFriction, settings.Acceleration, settings.Deacceleration, dt);
            var wishSpeed = math.length(direction) * settings.BaseSpeed;
            if (float.IsNaN(wishSpeed)) wishSpeed = 0;

            var strafeAngleNormalized = SrtGetStrafeAngleNormalized(direction, math.normalize(initialVelocity));
            var strafePower = math.max(math.max(1 - strafeAngleNormalized, 0.1f), 0.1f);
            strafePower = 0.1f;
            
            if (wishSpeed > settings.BaseSpeed && wishSpeed < currentSpeed)
            {
                wishSpeed = math.lerp(currentSpeed, wishSpeed, math.max(math.distance(wishSpeed, currentSpeed), 0) * dt);
            }

            velocity = SrtAccelerate(velocity, direction, wishSpeed, settings.Acceleration, strafePower, dt);
            
            return velocity;
        }

        private static float3 SrtFixNaN(float3 original)
        {
            for (int i = 0; i != 3; i++)
            {
                if (float.IsNaN(original[i])) original[i] = 0f;
            }

            return original;
        }

        /// <summary>
        /// Get the power of the friction from the player speed
        /// </summary>
        /// <param name="speed">The player speed</param>
        /// <param name="frictionSpeedMin">The minimal speed for friction to be start</param>
        /// <param name="frictionSpeedMax">The maximal speed for friction to be stop</param>
        /// <param name="frictionMin">The minimal friction (between 0 and 1)</param>
        /// <param name="frictionMax">The maximal friction (between 0 and 1)</param>
        /// <returns>Return the new friction power</returns>
        private static float SrtGetFrictionPower(float speed, float frictionSpeedMin, float frictionSpeedMax, float frictionMin, float frictionMax)
        {
            return Mathf.Clamp
            (
                frictionSpeedMin / Mathf.Clamp(speed, frictionSpeedMin, frictionSpeedMax),
                frictionMin, frictionMax
            );
        }

        private static float SrtGetStrafeAngleNormalized(Vector3 direction, Vector3 velocityDirection)
        {
            return math.max(math.clamp(Vector3.Angle(direction, velocityDirection), 1, 90) / 90f, 0f);
        }

        /// <summary>
        /// Apply the friction to a given velocity
        /// </summary>
        /// <param name="velocity">The player velocity</param>
        /// <param name="friction">The friction power to use</param>
        /// <param name="groundFriction">The friction power of the surface</param>
        /// <param name="accel">The acceleration of the player</param>
        /// <param name="deaccel">The deaceleration of the player</param>
        /// <param name="dt">The delta time</param>
        /// <returns>Return a new velocity from the friction</returns>
        private static float3 SrtApplyFriction(float3 velocity, float friction, float groundFriction, float accel, float deaccel, float dt)
        {
            var speed    = math.length(velocity);
            var control  = speed < accel ? deaccel : speed;
            var drop     = control * groundFriction * dt * friction;
            var newspeed = math.max(speed - drop, 0);

            if (speed > 0)
                newspeed /= speed;

            return velocity * newspeed;
        }

        /// <summary>
        /// Accelerate the player from a given velocity
        /// </summary>
        /// <param name="velocity">The player velocity</param>
        /// <param name="wishDirection">The wished direction</param>
        /// <param name="wishSpeed">The wished speed</param>
        /// <param name="accelPower">The acceleration power</param>
        /// <param name="strafePower">The strafe power (think of CPMA movement)</param>
        /// <param name="dt">The delta time</param>
        /// <returns>The new velocity from the acceleration</returns>
        private static float3 SrtAccelerate(float3 velocity, float3 wishDirection, float wishSpeed, float accelPower, float strafePower, float dt)
        {
            var speed = math.lerp(math.length(velocity), math.dot(velocity, wishDirection), strafePower);
            var addSpeed = wishSpeed - speed;
            if (addSpeed <= 0)
                return velocity;

            var factor = 1 - (1 / (wishSpeed / speed));
            if (float.IsNaN(factor) || factor <= 0.3f)
                factor = 0.3f;
            
            var accelSpeed = math.min((accelPower * factor) * dt * wishSpeed, addSpeed);
            if (float.IsNaN(accelSpeed))
                accelSpeed = 0f;

            return velocity + (accelSpeed * wishDirection);
        }
    }
}