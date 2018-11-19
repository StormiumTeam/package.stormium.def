using package.stormium.core;
using package.stormium.def.Movements.Data;
using package.stormiumteam.shared;
using Scripts.Movements.Data;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace package.stormium.def.Movements.Systems
{
    [UpdateAfter(typeof(DefStRunManageInputSystem))]
    [UpdateAfter(typeof(DefStJumpProcessSystem))]
    public class DefStAerialRunProcessSystem : ComponentSystem
    {
        struct Group
        {
            public ComponentDataArray<StVelocity> Velocities;
            public ComponentDataArray<DefStAerialRunSettings> Settings;
            public ComponentDataArray<DefStRunInput> Inputs;
            public ComponentDataArray<CharacterControllerState> State;
            public SubtractiveComponent<VoidSystem<DefStAerialRunProcessSystem>> Void1;
            public TransformAccessArray Transforms;

            public readonly int Length;
        }

        [Inject] private PhysicUpdaterSystem m_PhysicUpdaterSystem;
        [Inject] private Group m_Group;

        protected override void OnUpdate()
        {
            for (int frameIndex = 0; frameIndex != m_PhysicUpdaterSystem.LastIterationCount; frameIndex++)
            {
                SimulatePhysicStep(m_PhysicUpdaterSystem.LastFixedTimeStep);
            }
        }

        private void SimulatePhysicStep(float dt)
        {
            for (int i = 0; i != m_Group.Length; i++)
            {
                var velocityData = m_Group.Velocities[i];
                var input        = m_Group.Inputs[i];
                var state        = m_Group.State[i];
                var settings     = m_Group.Settings[i];
                var transform = m_Group.Transforms[i];

                if (state.IsGrounded()) continue;

                velocityData.Value = SrtFixNaN(velocityData.Value);

                var direction   = SrtComputeDirection(transform.rotation, input.Direction);
                var newVelocity = SrtMove(velocityData.Value, direction, settings, dt);
                newVelocity.y = velocityData.Value.y;

                m_Group.Velocities[i] = new StVelocity(newVelocity);
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
        /// Move the character with the Aerial Srt algorithm.
        /// </summary>
        /// <param name="initialVelocity">The initial velocity to use</param>
        /// <param name="direction">The movement direction</param>
        /// <param name="settings">The movement settings</param>
        /// <param name="dt">Delta time</param>
        /// <returns>Return the new position</returns>
        private static float3 SrtMove(float3 initialVelocity, float3 direction, DefStAerialRunSettings settings, float dt)
        {
            // Fix NaN errors
            direction = SrtFixNaN(direction);

            var wishSpeed = math.length(direction) * settings.BaseSpeed;
            var gridVelocity = math.float3(initialVelocity.x, 0, initialVelocity.z);
            var velocity = initialVelocity;

            velocity = SrtAirAccelerate(velocity, direction, settings.Acceleration, settings.Control, dt);
            var finalVelocity = SrtClampSpeed(math.float3(velocity.x, 0, velocity.z), gridVelocity, settings.BaseSpeed);
            var addSpeedFromHeight = math.clamp(-initialVelocity.y * (settings.AccelerationByHighsForce), 0, 1);
            
            return math.normalizesafe(finalVelocity) * (math.length(finalVelocity) + addSpeedFromHeight);
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
        /// Accelerate the player from a given velocity
        /// </summary>
        /// <param name="velocity">The player velocity</param>
        /// <param name="wishDirection">The wished direction</param>
        /// <param name="control">The control factor</param>
        /// <param name="dt">The delta time</param>
        /// <returns>The new velocity from the acceleration</returns>
        private static float3 SrtAirAccelerate(float3 velocity, float3 wishDirection, float acceleration, float control, float dt)
        {
            return velocity + (wishDirection * control * dt * acceleration);
        }
        
        private static float3 SrtClampSpeed(float3 velocity, float3 initialVelocity, float speed)
        {
            return Vector3.ClampMagnitude(velocity, math.max(math.length(initialVelocity), speed));
        }
    }
}