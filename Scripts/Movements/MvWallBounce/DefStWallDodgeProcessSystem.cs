using package.stormium.def.Movements.Data;
using package.stormium.def.Utilities;
using package.stormiumteam.shared;
using Scripts.Movements.MvWallBounce;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Input;

namespace package.stormium.def.Movements.Systems
{
    public class DefStWallDodgeProcessSystem : GameComponentSystem
    {
        private const float DefaultCooldown = 0.1f;

        struct Group
        {
            public ComponentDataArray<StVelocity>                                Velocities;
            public ComponentDataArray<DefStDodgeOnGroundSettings>                Settings;
            public ComponentDataArray<DefStRunInput>                             RunInputs;
            public ComponentDataArray<DefStDodgeInput>                           Inputs;
            public ComponentDataArray<DefStWallDodgeProcessData>                 Proccesses;
            public ComponentDataArray<DefStDodgeOnGroundProcessData>                 GroundProccesses;
            public ComponentArray<CharacterControllerMotor>                      Motors;
            public SubtractiveComponent<VoidSystem<DefStWallDodgeProcessSystem>> Void1;
            public EntityArray                                                   Entities;

            public readonly int Length;
        }

        [Inject] private Group m_Group;

        private Entity m_CmdDoDodge, m_CmdDoDodgeResult;

        protected override void OnCreateManager()
        {
            m_CmdDoDodge = CreateCommandTarget(ComponentType.Create<CmdMovement>(), typeof(CmdMvWallBounce), typeof(CmdMvWallDodge));

            m_CmdDoDodgeResult = CreateCommandResult(typeof(StVelocity));
        }

        protected override void OnUpdate()
        {
            for (int i = 0; i != m_Group.Length; i++)
            {
                var entity   = m_Group.Entities[i];
                var velocity = m_Group.Velocities[i];
                var setting  = m_Group.Settings[i];
                var runInput = m_Group.RunInputs[i];
                var input    = m_Group.Inputs[i];
                var process = m_Group.Proccesses[i];
                var groundProcess = m_Group.GroundProccesses[i];
                var motor = m_Group.Motors[i];

                ProcessItem(ref entity, ref velocity, ref setting, ref runInput, ref input, ref process, ref groundProcess, motor);
                
                PostUpdateCommands.SetComponent(entity, velocity);
                PostUpdateCommands.SetComponent(entity, setting);
                PostUpdateCommands.SetComponent(entity, input);
                PostUpdateCommands.SetComponent(entity, process);
                PostUpdateCommands.SetComponent(entity, groundProcess);
            }
        }

        private bool ProcessItem
        (
            ref Entity               entity,
            ref StVelocity        velocity,
            ref DefStDodgeOnGroundSettings    setting,
            ref DefStRunInput runInput,
            ref DefStDodgeInput       input,
            ref DefStWallDodgeProcessData process,
            ref DefStDodgeOnGroundProcessData groundProcess,
            CharacterControllerMotor motor
        )
        {
            var action = input.State != InputState.None && !MvUtils.OnGround(motor, velocity) && Time.time > process.TimeBeforeNextWD;
            
            DiffuseCommand(m_CmdDoDodge, m_CmdDoDodgeResult, action, CmdState.Begin);

            if (!m_CmdDoDodgeResult.GetComponentData<EntityCommandResult>().AsBool())
            {
                DiffuseCommand(m_CmdDoDodge, m_CmdDoDodgeResult, action, CmdState.End);
                
                return false;
            }

            var originalVelocity = velocity.Value;
            var fwd       = motor.transform.forward;
            var pos       = motor.transform.position + new Vector3(0, motor.CharacterController.stepOffset + 0.1f);
            var rot       = motor.transform.rotation;
            var rd        = motor.CharacterController.radius + 0.075f;
            var sw        = motor.CharacterController.skinWidth + 0.025f;
            var height    = motor.CharacterController.height - motor.CharacterController.stepOffset;
            var subheight = (height * 0.75f) - 0.005f;
            
            CPhysicSettings.Active.SetGlobalCollision(motor.gameObject, false);

            var direction = (Vector3)SrtComputeDirection(fwd, rot, runInput.Direction);
            direction = (velocity.Value.ToGrid(1).normalized + direction * 2).normalized;
            var rayTrace = UtilityWallRayTrace.RayTrace(ref direction, ref pos, ref rd, ref sw, ref height, ref subheight, motor.CharacterController);

            CPhysicSettings.Active.SetGlobalCollision(motor.gameObject, true);

            var success = rayTrace.normal != Vector3.zero && Mathf.Abs(rayTrace.normal.y) < 0.2f;
            if (success)
            {
                rayTrace.normal = rayTrace.normal.ToGrid(1).normalized;

                var reflected = (Vector3.Reflect(direction, rayTrace.normal) + rayTrace.normal).normalized;
                var oldY = velocity.Value.y;
                var dirInertie = (reflected * (velocity.Value.magnitude + 1)) + rayTrace.normal * 3.5f;
                dirInertie = RaycastUtilities.SlideVelocityNoYChange(velocity.Value, rayTrace.normal) + rayTrace.normal * 10;

                var minSpeed = Mathf.Max(velocity.Value.ToGrid(1).magnitude + setting.AdditiveSpeed, setting.MinSpeed);
                
                velocity.Value = dirInertie.ToGrid(1).normalized * (minSpeed);

                velocity.Value.y = Mathf.Max(oldY + 3, 0);
                
                process.TimeBeforeNextWD = Time.time + DefaultCooldown;

                input.TimeBeforeResetState = -1f;
                input.State = InputState.None;
                
                groundProcess.CooldownBeforeNextDodge = 0.25f;
                //groundProcess.InertieDelta            = 0.5f;
                groundProcess.Direction               = dirInertie.normalized;
                
                BroadcastNewEntity(PostUpdateCommands, true);
                PostUpdateCommands.AddComponent(new DefStWallJumpEvent(Time.time, Time.frameCount, entity, originalVelocity, rayTrace.normal));
            }
            
            DiffuseCommand(m_CmdDoDodge, m_CmdDoDodgeResult, action, CmdState.End);

            return success;
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