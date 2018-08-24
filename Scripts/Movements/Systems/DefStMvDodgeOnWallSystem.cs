using package.stormiumteam.shared;
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
    public class DefStMvDodgeOnWallSystem : ComponentSystem
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
                var wallDodge    = m_Group.WallDodgeComponents[i];
                var dodgeSetting = m_Group.DodgeSettings[i];
                var motor        = m_Group.Motors[i];
                var entity       = m_Group.Entities[i];

                var velocityData = m_Group.Velocities[i];

                wallDodge.Cooldown -= delta;

                if (input.WallDodge > 0.5f && !motor.IsGrounded()
                                           && wallDodge.Cooldown <= 0f
                                           && stamina.Value >= wallDodge.StaminaUse)
                {
                    var controller = motor.CharacterController;
                    var transform  = motor.transform;

                    var worldCenter     = transform.position + controller.center;
                    var radius          = controller.radius;
                    var skinWidth       = controller.skinWidth;
                    var height          = controller.height;
                    var substractHeight = 0.6f; //< StepOffset
                    var currVel         = velocityData.Velocity.ToGrid(1).normalized;
                    var direction       = motor.transform.TransformDirection(input.RunDirection);
                    direction   = Vector3.Lerp(direction, currVel, direction.magnitude);
                    direction.y = 0;

                    controller.enabled = false;
                    
                    var castResult = UtilityWallRayTrace.RayTrace
                    (
                        ref direction, ref worldCenter, ref radius, ref skinWidth, ref height, ref substractHeight, controller
                    );

                    controller.enabled = true;

                    var finalHeight = height - substractHeight;
                    var lowPoint    = worldCenter - new Vector3(0, finalHeight * 0.5f, 0);
                    if (castResult.normal != Vector3.zero)
                    {
                        var velocity = velocityData.Velocity;
                        var oldY     = velocity.y;
                        var dodgeDir = castResult.normal;

                        Debug.DrawRay(worldCenter, castResult.normal, Color.red, 20f);

                        var lerpT = Mathf.Clamp(Vector3.Distance(dodgeDir, direction) * 0.5f, 0f, 0.5f);
                        //lerpT = 0f;

                        //dodgeDir = Vector3.Lerp(dodgeDir, direction, lerpT * 0.5f);
                        dodgeDir.y *= 0f;
                        dodgeDir.Normalize();

                        //velocity += dodgeDir * (velocity.ToGrid(1).magnitude + dodgeSetting.AdditiveForce);
                        var dirNorm = currVel;
                        var reflectDirArg = Vector3.Lerp(dirNorm, dodgeDir, dirNorm.magnitude - Vector3.Distance(dirNorm, dodgeDir)) * 0.5f;
                        
                        //velocity += Vector3.Reflect(reflectDirArg, dodgeDir) * 4;
                        velocity += reflectDirArg + (dodgeDir * 1.5f * Mathf.Clamp(dirNorm.magnitude, 6, 12));

                        var oldVelocity = velocity;

                        velocity = velocity.normalized
                                   * Mathf.Clamp(velocity.ToGrid(1).magnitude + dodgeSetting.AdditiveForce,
                                       dodgeSetting.MinimumSpeed,
                                       wallDodge.MaximalSpeed);

                        Debug.Log($"{oldVelocity},,, {velocity}");

                        // Get the gravity
                        var gravity = Physics.gravity;
                        if (EntityManager.HasComponent<DefStMvGravity>(entity))
                        {
                            var gravityComponent = EntityManager.GetComponentData<DefStMvGravity>(entity);
                            gravity = gravityComponent.Mode == DefStMvGravity.GravityMode.Custom
                                ? gravityComponent.Gravity
                                : gravity;
                        }

                        velocity.y =  oldY;
                        velocity   -= gravity * wallDodge.VerticalBump;

                        velocityData.Velocity = velocity;

                        stamina.Value -= wallDodge.StaminaUse;

                        wallDodge.Cooldown = 0.25f;
                    }
                }

                if (motor.IsGrounded() && wallDodge.Cooldown > 0.1f) wallDodge.Cooldown = 0.1f;

                // I could have used the SET from the indexer, but sometime it throw some errors...
                // (as if the array was deallocated???)
                input.WallDodge = 0;
                
                PostUpdateCommands.SetComponent(entity, input);
                PostUpdateCommands.SetComponent(entity, stamina);
                PostUpdateCommands.SetComponent(entity, wallDodge);
                PostUpdateCommands.SetComponent(entity, velocityData);
            }
        }

        private struct Group
        {
            public ComponentDataArray<StCharacter>                  Characters;
            public ComponentDataArray<DefStMvDodgeOnWallExecutable> ExecuteFlags;
            public ComponentDataArray<DefStMvInput>                 Inputs;
            public ComponentDataArray<DefStMvDodgeOnWall>           WallDodgeComponents;
            public ComponentDataArray<DefStMvDodge>                 DodgeSettings;
            public ComponentDataArray<DefStMvStamina>               Staminas;
            public ComponentDataArray<DefStVelocity>                Velocities;
            public ComponentArray<CharacterControllerMotor>         Motors;
            public EntityArray                                      Entities;

            public readonly int Length;
        }
    }
}