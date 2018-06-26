using package.guerro.shared;
using package.stormium.core;
using package.stormium.def.Utilities;
using Unity.Entities;
using UnityEngine;

namespace package.stormium.def
{
    [UpdateAfter(typeof(STUpdateOrder.UOMovementUpdate.Loop))]
    [UpdateBefore(typeof(STUpdateOrder.UOMovementUpdate.FixMovement))]
    [UpdateAfter(typeof(DefStMvGravitySystem))]
    [UpdateAfter(typeof(DefStMvRunSystem))]
    public class DefStMvWalljumpSystem : ComponentSystem
    {
        [Inject] private Group m_Group;

        protected override void OnUpdate()
        {
            OnSimulationUpdate(Time.deltaTime);
        }

        private void OnSimulationUpdate(float delta)
        {
            for (var i = 0; i != m_Group.Length; i++)
            {
                var input        = m_Group.Inputs[i];
                var stamina      = m_Group.Staminas[i];
                var wallJump    = m_Group.WalljumpComponents[i];
                var motor        = m_Group.Motors[i];
                var entity       = m_Group.Entities[i];

                var velocityData = m_Group.Velocities[i];

                wallJump.Cooldown -= delta;

                if (input.WallJump > 0.5f && !motor.IsGrounded()
                                       && wallJump.Cooldown <= 0f
                                       && stamina.Value >= wallJump.StaminaUse)
                {
                    var controller = motor.CharacterController;
                    var transform  = motor.transform;

                    var worldCenter = transform.position + controller.center;
                    var radius      = controller.radius;
                    var skinWidth   = controller.skinWidth;
                    var height = controller.height;
                    var substractHeight = 0.3f; //< StepOffset
                    var direction  = motor.transform.TransformDirection(input.RunDirection);
                    direction = Vector3.Lerp(direction, velocityData.Velocity.ToGrid(1).normalized, 1 - direction.magnitude);
                    
                    Debug.DrawRay(worldCenter, direction, Color.blue, 0.25f);

                    controller.enabled = false;
                    
                    var castResult = UtilityWallRayTrace.RayTrace
                    (
                        ref direction, ref worldCenter, ref radius, ref skinWidth, ref height, ref substractHeight
                    );

                    controller.enabled = true;

                    var finalHeight = height - substractHeight;
                    var lowPoint    = worldCenter - new Vector3(0, finalHeight * 0.5f, 0);
                    if (castResult.normal != Vector3.zero
                        && castResult.normal.y < 0.01f)
                    {
                        var velocity = velocityData.Velocity;
                        var oldY = velocity.y;
                        var dodgeDir = castResult.normal;

                        var lerpT = Mathf.Clamp(Vector3.Distance(dodgeDir, direction) * 0.5f, 0f, 0.5f);
                        lerpT = 0f;
                        
                        dodgeDir   =  Vector3.Lerp(dodgeDir, direction, lerpT * 0.5f);
                        dodgeDir.y *= 0f;
                        dodgeDir.Normalize();
                        
                        velocity += dodgeDir * (wallJump.WallForce);
                        velocity += direction * (wallJump.DirectionForce);
                        /*velocity = velocity.normalized 
                                   * Mathf.Clamp(velocity.ToGrid(1).magnitude + dodgeSetting.AdditiveForce,
                                       dodgeSetting.MinimumSpeed,
                                       wallDodge.MaximalSpeed);*/

                        // Get the gravity
                        var gravity = Physics.gravity;
                        if (EntityManager.HasComponent<DefStMvGravity>(entity))
                        {
                            var gravityComponent = EntityManager.GetComponentData<DefStMvGravity>(entity);
                            gravity = gravityComponent.Mode == DefStMvGravity.GravityMode.Custom
                                ? gravityComponent.Gravity
                                : gravity;
                        }

                        oldY = Mathf.Max(0, oldY);

                        velocity.y = oldY;
                        velocity -= gravity * wallJump.VerticalBump;
                        
                        velocityData.Velocity = velocity;
                        
                        stamina.Value -= wallJump.StaminaUse;

                        wallJump.Cooldown = 0.25f;
                    }
                }

                if (motor.IsGrounded() && wallJump.Cooldown > 0.1f) wallJump.Cooldown = 0.1f;

                m_Group.Staminas[i]            = stamina;
                m_Group.WalljumpComponents[i] = wallJump;
                m_Group.Velocities[i]          = velocityData;
            }
        }

        private struct Group
        {
            public ComponentDataArray<StCharacter>          Characters;
            public ComponentDataArray<DefStMvInput>         Inputs;
            public ComponentDataArray<DefStMvWalljump>   WalljumpComponents;
            public ComponentDataArray<DefStMvStamina>       Staminas;
            public ComponentDataArray<DefStVelocity>        Velocities;
            public ComponentArray<CharacterControllerMotor> Motors;
            public EntityArray                              Entities;

            public int Length;
        }
    }
}
