using package.stormium.def.Movements.Data;
using package.stormium.def.Utilities;
using package.stormiumteam.shared;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Input;

namespace package.stormium.def.Movements.Systems
{
    public class DefStWallJumpProcessSystem : ComponentSystem
    {
        private const float DefaultCooldown = 0.1f;
        
        struct Group
        {
            public ComponentDataArray<StVelocity>                           Velocities;
            public ComponentDataArray<DefStJumpSettings>                    Settings;
            public ComponentDataArray<DefStRunInput>                        RunInputs;
            public ComponentDataArray<DefStJumpInput>                       Inputs;
            public ComponentDataArray<DefStWallJumpProcessData>             Proccesses;
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
                var runInput = m_Group.RunInputs[i];
                var input    = m_Group.Inputs[i];
                var process = m_Group.Proccesses[i];
                var motor = m_Group.Motors[i];

                ProcessItem(ref entity, ref velocity, ref setting, ref runInput, ref input, ref process, motor);
                
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
            ref DefStWallJumpProcessData process,
            CharacterControllerMotor motor
        )
        {
            var action = input.State != InputState.None && !motor.IsGrounded() && Time.time > process.TimeBeforeNextWJ;
            if (!action)
                return false;

            var fwd = motor.transform.forward;
            var pos = motor.transform.position + new Vector3(0, motor.CharacterController.stepOffset + 0.1f);
            var rot = motor.transform.rotation;
            var rd = motor.CharacterController.radius + 0.075f;
            var sw = motor.CharacterController.skinWidth + 0.025f;
            var height = motor.CharacterController.height - motor.CharacterController.stepOffset;
            var subheight = (height * 0.75f) - 0.005f;
            
            CPhysicSettings.Active.SetGlobalCollision(motor.gameObject, false);

            var direction = (Vector3)SrtComputeDirection(fwd, rot, runInput.Direction);
            var rayTrace = UtilityWallRayTrace.RayTrace(ref direction, ref pos, ref rd, ref sw, ref height, ref subheight, motor.CharacterController);
            
            Debug.DrawRay(rayTrace.point, rayTrace.normal, Color.red, 10);
            
            CPhysicSettings.Active.SetGlobalCollision(motor.gameObject, true);

            var success = rayTrace.normal != Vector3.zero && Mathf.Abs(rayTrace.normal.y) < 0.2f;
            if (success)
            {
                rayTrace.normal = rayTrace.normal.ToGrid(1).normalized;
                
                var gravity = GetGravity(entity, setting);
                velocity.Value = RaycastUtilities.SlideVelocityNoYChange(velocity.Value, rayTrace.normal);

                velocity.Value -= gravity * (setting.JumpPower * 0.98f);

                var previousVelocity = velocity.Value;
                var bounceDir = rayTrace.normal * 6.5f;
                var minSpeed = bounceDir.magnitude;
                
                velocity.Value += bounceDir;
                
                var flatVelocity = velocity.Value.ToGrid(1);
                var oldY = velocity.Value.y;

                velocity.Value = Vector3.ClampMagnitude(flatVelocity, Mathf.Max(previousVelocity.magnitude, minSpeed));
                
                velocity.Value.y = oldY;

                process.TimeBeforeNextWJ = Time.time + DefaultCooldown;

                input.TimeBeforeResetState = -1f;
                input.State = InputState.None;
            }

            return success;
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