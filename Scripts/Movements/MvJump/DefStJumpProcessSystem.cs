using package.stormium.def.Movements.Data;
using package.stormium.def.Utilities;
using package.stormiumteam.shared;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Input;
using UnityEngine.Jobs;

namespace package.stormium.def.Movements.Systems
{
    [UpdateAfter(typeof(DefStDodgeOnGroundProcessSystem))]
    public class DefStJumpProcessSystem : GameComponentSystem
    {
        struct Group
        {
            public ComponentDataArray<StVelocity>                        Velocities;
            public ComponentDataArray<DefStJumpSettings>                    Settings;
            public ComponentDataArray<DefStRunInput>                       RunInputs;
            public ComponentDataArray<DefStJumpInput>                       Inputs;
            public ComponentDataArray<DefStJumpProcessData>                 Proccesses;
            public ComponentDataArray<CharacterControllerState>                 States;
            public SubtractiveComponent<VoidSystem<DefStJumpProcessSystem>> Void1;
            public EntityArray                                              Entities;
            public TransformAccessArray Transforms;

            public readonly int Length;
        }

        [Inject] private Group m_Group;

        private Vector3         m_CachedDefaultGravity;

        private Entity m_CmdDoJump, m_CmdDoJumpResult;

        protected override void OnCreateManager()
        {
            m_CmdDoJump = CreateCommandTarget(ComponentType.Create<CmdMovement>(), typeof(CmdMvJump));
            m_CmdDoJumpResult = CreateCommandResult(typeof(StVelocity));
        }

        protected override void OnUpdate()
        {
            m_CachedDefaultGravity = Physics.gravity;

            for (int i = 0; i != m_Group.Length; i++)
            {
                var entity   = m_Group.Entities[i];
                var velocity = m_Group.Velocities[i];
                var setting  = m_Group.Settings[i];
                var runInput = m_Group.RunInputs[i];
                var input    = m_Group.Inputs[i];
                var process = m_Group.Proccesses[i];
                var state = m_Group.States[i];
                var transform = m_Group.Transforms[i];

                ProcessItem(ref entity, ref velocity, ref setting, ref runInput, ref input, ref process, ref state, transform);
                
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
            ref DefStRunInput runInput,
            ref DefStJumpInput       input,
            ref DefStJumpProcessData process,
            ref CharacterControllerState state,
            Transform transform
        )
        {
            var doJump = input.State != InputState.None && (process.ComboCtx < setting.MaxCombo && process.CooldownBeforeNextJump <= 0f)
                                                        && (MvUtils.OnGround(state, velocity) || process.ComboCtx > 0);
            var airJump = doJump && !state.IsGrounded() && !state.IsSliding();

            if (input.TimeBeforeResetState <= 0f)
            {
                input.State = InputState.None;
            }

            if (state.IsStableOnGround())
            {
                process.ComboCtx = 0;
            }
            
            process.CooldownBeforeNextJump -= Time.deltaTime;
            input.TimeBeforeResetState -= Time.deltaTime;
            
            // We expect the developpers to check for staminas or things like that for this command.
            DiffuseCommand(m_CmdDoJump, m_CmdDoJumpResult, doJump, CmdState.Begin);

            doJump = GetCmdResult(m_CmdDoJumpResult);
            if (!doJump)
                return false;

            var direction = SrtComputeDirection(transform.forward, transform.rotation, runInput.Direction);
            var strafeAngle = SrtGetStrafeAngleNormalized(direction, velocity.Value);
            if (math.all(runInput.Direction == float2.zero))
            {
                strafeAngle = 0f;
            }
            
            velocity.Value.y = math.max(velocity.Value.y, 0);
            if (!airJump) velocity.Value += Vector3.up * setting.JumpPower;
            else velocity.Value = Vector3.up * setting.JumpPower;

            if (airJump)
            {
                velocity.Value = math.lerp(velocity.Value, SrtAirDash(velocity.Value, direction), 1f);
            }
            else
            {
                //velocity.Value += (Vector3)(direction * (strafeAngle * 3.5f));
                var oldY = velocity.Value.y;
                var currSpeed = velocity.Value.ToGrid(1).magnitude;
                var newSpeed = math.min(currSpeed + strafeAngle * 12.5f, math.max(currSpeed, 17.5f));
                velocity.Value = velocity.Value.normalized * newSpeed;
                velocity.Value.y = oldY;
            }

            input.TimeBeforeResetState = -1f;
            input.State = InputState.None;

            process.ComboCtx++;
            process.CooldownBeforeNextJump = 0.1f;
            
            // We except developpers to just clean the pre-command phase, and not applying things like reducing stamina...
            DiffuseCommand(m_CmdDoJump, m_CmdDoJumpResult, true, CmdState.End);
            
            // Send event to clients
            BroadcastNewEntity(PostUpdateCommands, true);
            PostUpdateCommands.AddComponent(new DefStJumpEvent(Time.time, Time.frameCount, entity));
            
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
        private static float3 SrtComputeDirection(Vector3 worldForward, Quaternion worldRotation, float2 inputDirection)
        {
            var inputDirection3D = Vector3.Normalize(worldRotation * new Vector3(inputDirection.x, 0, inputDirection.y));

            return Vector3.Normalize(Vector3.Lerp(worldForward, inputDirection3D, 1 - (worldForward.magnitude - inputDirection3D.magnitude)));
        }
        
        private static float SrtGetStrafeAngleNormalized(Vector3 direction, Vector3 velocityDirection)
        {
            return math.max(math.clamp(Vector3.Angle(direction, velocityDirection), 1, 90) / 90f, 0f);
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