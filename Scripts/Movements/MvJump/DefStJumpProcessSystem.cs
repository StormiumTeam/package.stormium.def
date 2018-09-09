using package.stormium.def.Movements.Data;
using package.stormiumteam.shared;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Input;

namespace package.stormium.def.Movements.Systems
{
    public class DefStJumpProcessSystem : ComponentSystem
    {
        struct Group
        {
            public ComponentDataArray<StVelocity>                        Velocities;
            public ComponentDataArray<DefStJumpSettings>                    Settings;
            public ComponentDataArray<DefStJumpInput>                       Inputs;
            public ComponentDataArray<DefStJumpProcessData>                 Proccesses;
            public ComponentArray<CharacterControllerMotor>                 Motors;
            public SubtractiveComponent<VoidSystem<DefStJumpProcessSystem>> Void1;
            public EntityArray                                              Entities;

            public readonly int Length;
        }

        [Inject] private Group m_Group;

        private Vector3         m_CachedDefaultGravity;

        protected override void OnUpdate()
        {
            m_CachedDefaultGravity = Physics.gravity;

            for (int i = 0; i != m_Group.Length; i++)
            {
                var entity   = m_Group.Entities[i];
                var velocity = m_Group.Velocities[i];
                var setting  = m_Group.Settings[i];
                var input    = m_Group.Inputs[i];
                var process = m_Group.Proccesses[i];
                var motor = m_Group.Motors[i];

                ProcessItem(ref entity, ref velocity, ref setting, ref input, ref process, motor);
                
                PostUpdateCommands.SetComponent(entity, velocity);
                PostUpdateCommands.SetComponent(entity, setting);
                PostUpdateCommands.SetComponent(entity, input);
                PostUpdateCommands.SetComponent(entity, process);
            }
        }

        private bool ProcessItem
        (
            ref Entity               entity,
            ref StVelocity        velocity,
            ref DefStJumpSettings    setting,
            ref DefStJumpInput       input,
            ref DefStJumpProcessData process,
            CharacterControllerMotor motor
        )
        {
            var doJump = input.State != InputState.None && (process.ComboCtx < setting.MaxCombo && process.CooldownBeforeNextJump <= 0f)
                                                        && (motor.IsGrounded() || process.ComboCtx > 0);
            var airJump = doJump && !motor.IsGrounded() && !motor.IsSliding;

            if (input.TimeBeforeResetState <= 0f)
            {
                input.State = InputState.None;
            }

            if (motor.IsStableOnGround)
            {
                process.ComboCtx = 0;
            }

            process.CooldownBeforeNextJump -= Time.deltaTime;
            input.TimeBeforeResetState -= Time.deltaTime;

            if (!doJump)
                return false;

            var gravity = GetGravity(entity, setting);
            
            velocity.Value.y = math.max(velocity.Value.y, 0);
            if (!airJump) velocity.Value -= gravity * setting.JumpPower;
            else velocity.Value -= gravity * (setting.JumpPower * 0.25f);

            if (airJump && entity.HasComponent<DefStRunInput>())
            {
                var runInput = entity.GetComponentData<DefStRunInput>();
                var direction = SrtComputeDirection(motor.transform.rotation, runInput.Direction);

                velocity.Value = math.lerp(velocity.Value, SrtAirDash(velocity.Value, direction), 1f);
            }
            else 
                motor.MoveBy(Vector3.up * 0.01f);

            input.TimeBeforeResetState = -1f;
            input.State = InputState.None;

            process.ComboCtx++;
            process.CooldownBeforeNextJump = 0.1f;
            
            return true;
        }

        private Vector3 GetGravity(Entity entity, DefStJumpSettings setting)
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
        private static float3 SrtComputeDirection(Quaternion worldRotation, float2 inputDirection)
        {
            inputDirection = math.select(inputDirection, float2.zero, math.isnan(inputDirection));
            
            return Vector3.Normalize(worldRotation * new Vector3(inputDirection.x, 0, inputDirection.y));
        }

        private static float3 SrtAirDash(float3 velocity, float3 wishDirection)
        {
            if (math.length(wishDirection) < 0.1f)
                wishDirection = math.normalize(velocity);
            
            var gridVelocity = math.float3(velocity.x, 0, velocity.z);
            
            var previousY         = velocity.y;
            var previousSpeed     = math.length(gridVelocity);
            var predictedVelocity = velocity + wishDirection;
            velocity += wishDirection * (math.length(gridVelocity) + 4);

            gridVelocity = math.float3(velocity.x, 0, velocity.z);

            velocity = Vector3.ClampMagnitude(gridVelocity, Mathf.Min(previousSpeed, math.length(gridVelocity)) + 1f);
            velocity = Vector3.Lerp(velocity, predictedVelocity, 0.1f);
            velocity.y = previousY;

            return velocity;
        }
    }
}