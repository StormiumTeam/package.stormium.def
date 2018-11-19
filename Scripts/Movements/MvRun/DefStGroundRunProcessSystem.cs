using package.stormium.core;
using package.stormium.def.Movements.Data;
using package.stormium.def.Utilities;
using package.stormiumteam.shared;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Profiling;

namespace package.stormium.def.Movements.Systems
{
    [UpdateAfter(typeof(DefStRunManageInputSystem))]
    [UpdateAfter(typeof(DefStJumpProcessSystem))]
    public class DefStGroundRunProcessSystem : ComponentSystem
    {
        private static int frame;
        
        struct Group
        {
            public ComponentDataArray<StVelocity>                                Velocities;
            public ComponentDataArray<DefStGroundRunSettings>                    Settings;
            public ComponentDataArray<DefStRunInput>                             Inputs;
            public ComponentDataArray<CharacterControllerState>                  State;
            public TransformAccessArray                                          Transforms;
            public SubtractiveComponent<VoidSystem<DefStGroundRunProcessSystem>> Void1;

            public readonly int Length;
        }

        [Inject] private PhysicUpdaterSystem m_PhysicUpdaterSystem;
        [Inject] private Group               m_Group;

        private NativeArray<quaternion> m_Rotations;
        private NativeArray<byte>       m_AuthorizedIndexes;

        protected override void OnCreateManager()
        {
            m_Rotations         = new NativeArray<quaternion>(0, Allocator.Persistent);
            m_AuthorizedIndexes = new NativeArray<byte>(0, Allocator.Persistent);
        }

        protected override void OnDestroyManager()
        {
            m_Rotations.Dispose();
            m_AuthorizedIndexes.Dispose();
        }

        protected override void OnUpdate()
        {
            frame = Time.frameCount;
            
            if (m_Rotations.Length != m_Group.Length)
            {
                m_Rotations.Dispose();
                m_AuthorizedIndexes.Dispose();

                m_Rotations         = new NativeArray<quaternion>(m_Group.Length, Allocator.Persistent);
                m_AuthorizedIndexes = new NativeArray<byte>(m_Group.Length, Allocator.Persistent);
            }

            new JobFillRotation(m_Rotations).Schedule(m_Group.Transforms).Complete();

            for (int frameIndex = 0; frameIndex != m_PhysicUpdaterSystem.LastIterationCount; frameIndex++)
            {
                SimulatePhysicStep(frameIndex, m_PhysicUpdaterSystem.LastFixedTimeStep);
            }
        }

        private void SimulatePhysicStep(int frameIndex, float dt)
        {
            var job = new JobCalculateMovement
            (
                m_Group.State, m_Group.Velocities, m_Group.Settings, m_Group.Inputs, m_Rotations, dt
            );
            job.Run(m_Group.Length);
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
        private static float3 SrtMove(float3 initialVelocity, float2 initialDirection, float3 direction, DefStGroundRunSettings settings, CharacterControllerState state,
                                      float dt)
        {
            // Fix NaN errors
            direction = SrtFixNaN(direction);

            // Set Y axe to zero
            initialVelocity.y = 0;

            var previousSpeed = math.length(initialVelocity);
            var friction = SrtGetFrictionPower
            (
                previousSpeed,
                settings.FrictionSpeedMin, settings.FrictionSpeedMax,
                settings.FrictionMin, settings.FrictionMax
            );

            var angleInvertedDir = 1 - state.AngleDir.y;
            if (math.abs(angleInvertedDir) > 0.4f)
                friction *= math.clamp(math.abs(angleInvertedDir), 0.4f, 1f);

            var velocity = SrtApplyFriction(initialVelocity, direction, friction, settings.SurfaceFriction, settings.Acceleration,
                settings.Deacceleration, dt);
            var wishSpeed                         = math.length(direction) * settings.BaseSpeed;
            if (float.IsNaN(wishSpeed)) wishSpeed = 0;

            var strafeAngleNormalized = SrtGetStrafeAngleNormalized(direction, math.normalize(initialVelocity));

            if (wishSpeed > settings.BaseSpeed && wishSpeed < previousSpeed)
            {
                wishSpeed = math.lerp(previousSpeed, wishSpeed, math.max(math.distance(wishSpeed, previousSpeed), 0) * dt);
            }

            
            if (initialDirection.y > 0.5f) {
                if (previousSpeed >= settings.BaseSpeed - 0.25f)
                {
                    wishSpeed = settings.SprintSpeed;

                    settings.Acceleration = 6f;
                }
            }
            
            if (state.AngleDir.y >= 0) wishSpeed *= math.clamp(state.AngleDir.y + 0.1f, 0.1f, 1f);
            
            velocity = SrtAccelerate(velocity, direction, wishSpeed, settings.Acceleration, math.min(strafeAngleNormalized, 0.25f), dt);

            var nextSpeed = math.length(math.float3(velocity.x, 0, velocity.z));
            if (previousSpeed > nextSpeed && nextSpeed < settings.BaseSpeed
                                          && strafeAngleNormalized > 0.1f && strafeAngleNormalized < 0.9f)
            {
                velocity.y = 0;

                velocity = Vector3.Normalize(velocity) * math.lerp(nextSpeed, previousSpeed, math.max(1 - strafeAngleNormalized, 0.8f));
            }

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
        private static float3 SrtApplyFriction(float3 velocity, float3 direction, float friction, float groundFriction, float accel, float deaccel, float dt)
        {
            direction = math.normalizesafe(direction);

            var frictionned = Vector3.MoveTowards(velocity, Vector3.zero, dt * groundFriction * friction);
            var defrictionned = Vector3.Lerp(velocity, frictionned, (1 - math.length(direction)));

            if (defrictionned.magnitude > 12)
                defrictionned = Vector3.MoveTowards(defrictionned, frictionned, dt * deaccel);

            velocity = defrictionned;

            return velocity;
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

            var nextSpeed = speed + (accelPower * dt);
            if (nextSpeed >= wishSpeed && speed <= wishSpeed)
                nextSpeed = wishSpeed;

            if (math.length(wishDirection) < 0.5f)
                return velocity;

            if (nextSpeed <= wishSpeed)
            {
                var c = 10f;
                if (wishSpeed >= 10)
                    c = 1.25f;
                
                return Vector3.MoveTowards(math.normalizesafe(velocity), wishDirection, dt * c * accelPower) * nextSpeed;
            }

            return Vector3.ClampMagnitude(velocity + wishDirection * (accelPower * dt), speed);
        }

        [BurstCompile]
        private struct JobFillRotation : IJobParallelForTransform
        {
            private NativeArray<quaternion> m_Rotations;

            public void Execute(int index, TransformAccess transform)
            {
                m_Rotations[index] = transform.rotation;
            }

            public JobFillRotation(NativeArray<quaternion> rotations)
            {
                m_Rotations = rotations;
            }
        }

        //[BurstCompile]
        private struct JobCalculateMovement : IJobParallelFor
        {
            private            ComponentDataArray<StVelocity>               Velocities;
            [ReadOnly] private ComponentDataArray<CharacterControllerState> States;
            [ReadOnly] private ComponentDataArray<DefStGroundRunSettings>   Settings;
            [ReadOnly] private ComponentDataArray<DefStRunInput>            Inputs;
            [ReadOnly] private NativeArray<quaternion>                      Rotations;
            [ReadOnly] private float                                        DeltaTime;

            public void Execute(int index)
            {
                if (States[index].GroundFlags == 0 || Velocities[index].Value.y > 0f)
                    return;

                var rotation     = Rotations[index];
                var setting      = Settings[index];
                var input        = Inputs[index];
                var velocityData = Velocities[index];

                velocityData.Value = SrtFixNaN(velocityData.Value);

                var direction   = SrtComputeDirection(rotation, input.Direction);
                var newVelocity = SrtMove(velocityData.Value, input.Direction, direction, setting, States[index], DeltaTime);

                newVelocity.y = velocityData.Value.y;

                Velocities[index] = new StVelocity(newVelocity);
            }

            public JobCalculateMovement(ComponentDataArray<CharacterControllerState> states,
                                        ComponentDataArray<StVelocity>               velocities,
                                        ComponentDataArray<DefStGroundRunSettings>   settings,
                                        ComponentDataArray<DefStRunInput>            inputses,
                                        NativeArray<quaternion>                      rotations,
                                        float                                        deltaTime)
            {
                States     = states;
                Velocities = velocities;
                Settings   = settings;
                Inputs     = inputses;
                Rotations  = rotations;
                DeltaTime  = deltaTime;
            }
        }
    }
}