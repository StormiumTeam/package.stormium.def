﻿using package.stormium.core;
using package.stormium.def.Movements.Data;
using package.stormiumteam.networking.ecs;
using package.stormiumteam.shared;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Input;

namespace package.stormium.def.Movements.Systems
{
    [UpdateAfter(typeof(DefStGroundRunProcessSystem))]
    public class DefStDodgeOnGroundProcessSystem : GameComponentSystem
    {
        struct Group
        {
            public ComponentDataArray<StVelocity>                          Velocities;
            public ComponentDataArray<DefStRunInput>                          RunInputs;
            public ComponentDataArray<DefStDodgeOnGroundSettings>             Settings;
            public ComponentDataArray<DefStDodgeInput>                        Inputs;
            public ComponentDataArray<DefStDodgeOnGroundProcessData>          Proccesses;
            public ComponentArray<CharacterControllerMotor>                   Motors;
            public SubtractiveComponent<VoidSystem<DefStDodgeOnGroundProcessSystem>> Void1;
            public EntityArray                                                Entities;

            public readonly int Length;
        }

        [Inject] private Group m_Group;
        [Inject] private PhysicUpdaterSystem m_PhysicUpdaterSystem;

        private Vector3         m_CachedDefaultGravity;

        protected override void OnUpdate()
        {
            m_CachedDefaultGravity = Physics.gravity;

            for (int i = 0; i != m_Group.Length; i++)
            {
                var entity   = m_Group.Entities[i];
                var velocity = m_Group.Velocities[i];
                var runInput = m_Group.RunInputs[i];
                var setting  = m_Group.Settings[i];
                var input    = m_Group.Inputs[i];
                var process  = m_Group.Proccesses[i];
                var motor    = m_Group.Motors[i];

                ProcessItem(ref entity, ref velocity, ref runInput, ref setting, ref input, ref process, motor);
                for (int frameIndex = 0; frameIndex != m_PhysicUpdaterSystem.LastIterationCount; frameIndex++)
                {
                    ProcessPhysicItem(m_PhysicUpdaterSystem.LastFixedTimeStep, ref setting, ref input, ref process, ref velocity, motor);
                }

                PostUpdateCommands.SetComponent(entity, velocity);
                PostUpdateCommands.SetComponent(entity, setting);
                PostUpdateCommands.SetComponent(entity, input);
                PostUpdateCommands.SetComponent(entity, process);
            }
        }

        private bool ProcessItem
        (
            ref Entity                        entity,
            ref StVelocity                 velocity,
            ref DefStRunInput                 runInput,
            ref DefStDodgeOnGroundSettings    setting,
            ref DefStDodgeInput               input,
            ref DefStDodgeOnGroundProcessData process,
            CharacterControllerMotor          motor
        )
        {
            var doDodge = input.State != InputState.None && process.CooldownBeforeNextDodge <= 0f && motor.IsGrounded();

            if (input.TimeBeforeResetState <= 0f)
            {
                input.State = InputState.None;
            }

            if (!doDodge)
                return false;

            process.StartFlatSpeed = velocity.Value.ToGrid(1).magnitude;
            
            var gravity   = GetGravity(entity, setting);
            var direction = SrtComputeDirection(motor.transform.forward.normalized, motor.transform.rotation, runInput.Direction);

            velocity.Value.y = 0f;

            velocity.Value = SrtDodge(velocity.Value, direction, setting.AdditiveSpeed, setting.MinSpeed, setting.MaxSpeed);

            velocity.Value -= gravity * setting.VerticalPower;

            motor.MoveBy(Vector3.up * 0.01f);
            motor.MoveBy(direction * 0.1f);

            input.TimeBeforeResetState = -1f;
            input.State                = InputState.None;

            process.StartFlatSpeed = Mathf.Max(process.StartFlatSpeed, setting.MinSpeed);
            process.CooldownBeforeNextDodge = 0.5f;
            process.InertieDelta = 1f;
            process.Direction               = direction;
            process.IsDodging = 1;
            
            // Send dodge message to clients
            if (IsConnectedOrHosting)
            {
                var evEntity = EntityManager.CreateEntity();
                ServerEntityMgr.NetworkifyAndPush(evEntity, PushEntityOption.CreateEntity);
                ServerEntityMgr.SyncSetOrAddComponent(evEntity, new DefStDodgeEvent(Time.time, Time.frameCount, entity));
            }

            return true;
        }

        private void ProcessPhysicItem
        (
            float                             dt,
            ref DefStDodgeOnGroundSettings settings,
            ref DefStDodgeInput               input,
            ref DefStDodgeOnGroundProcessData process,
            ref StVelocity velocity,
            CharacterControllerMotor          motor
        )
        {
            if (process.InertieDelta > 0f && math.any(process.Direction != float3.zero))
            {
                if (2 == 1)
                {
                    var previousVelocity = velocity.Value;
                    var previousFlatVel  = previousVelocity.ToGrid(1);
                    var previousSpeed    = Mathf.Max(previousFlatVel.magnitude, settings.MinSpeed) + (process.InertieDelta * dt);

                    velocity.Value   = math.normalize(process.Direction) * previousSpeed;
                    velocity.Value.y = previousVelocity.y;
                }
                else
                {
                    var factor = process.InertieDelta;

                    motor.MoveBy(process.Direction * factor * dt);
                }
            }

            if (motor.IsGrounded() && process.IsDodging == 1)
            {
                process.IsDodging = 0;

                var oldY = velocity.Value.y;
                var speed = Mathf.Min(velocity.Value.ToGrid(1).magnitude, process.StartFlatSpeed);

                velocity.Value = velocity.Value.normalized * speed;
                
                velocity.Value.y = oldY;
            }

            process.InertieDelta -= motor.IsGrounded() ? dt * 50f : dt;
            process.CooldownBeforeNextDodge -= dt;
            input.TimeBeforeResetState      -= dt;
        }

        private Vector3 GetGravity(Entity entity, DefStDodgeOnGroundSettings setting)
        {
            if (setting.GravityGravityType == GravityType.Custom)
                return setting.Gravity;
            if (setting.GravityGravityType == GravityType.Default)
                return m_CachedDefaultGravity;

            if (EntityManager.HasComponent<StGravitySettings>(entity))
            {
                var data = EntityManager.GetComponentData<StGravitySettings>(entity);
                return data.FlagIsDefault == 1 ? m_CachedDefaultGravity : data.Value;
            }

            return m_CachedDefaultGravity;
        }

        /// <summary>
        /// Compute the direction from a rotation and from a given direction
        /// </summary>
        /// <param name="worldRotation">The character rotation</param>
        /// <param name="inputDirection">The direction input</param>
        /// <returns>The new direction</returns>
        private static float3 SrtComputeDirection(Vector3 worldForward, Quaternion worldRotation, float2 inputDirection)
        {
            var inputDirection3D = Vector3.Normalize(worldRotation * new Vector3(inputDirection.x, 0, inputDirection.y));

            return Vector3.Normalize(Vector3.Lerp(worldForward, inputDirection3D, 1 - (worldForward.magnitude - inputDirection3D.magnitude)));
        }

        private static float3 SrtDodge(Vector3 velocity, Vector3 wishDirection, float addForce, float minSpeed, float maxSpeed)
        {
            var oldY          = velocity.y;
            var previousSpeed = velocity.ToGrid(1).magnitude;
            velocity   += wishDirection * (velocity.ToGrid(1).magnitude + addForce);
            velocity   =  Vector3.ClampMagnitude(velocity.ToGrid(1), Mathf.Min(previousSpeed + addForce, velocity.ToGrid(1).magnitude));
            velocity.y =  oldY;

            var speed = Mathf.Min(Mathf.Max(velocity.ToGrid(1).magnitude, minSpeed), maxSpeed);

            velocity = wishDirection * speed;
            velocity.y = oldY;

            return velocity;

            return velocity.normalized * speed;
        }
    }
}