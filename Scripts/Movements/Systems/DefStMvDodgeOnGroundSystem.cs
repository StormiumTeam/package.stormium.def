using package.stormiumteam.shared;
using package.stormium.core;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace package.stormium.def
{
    [UpdateAfter(typeof(STUpdateOrder.UOMovementUpdate.Loop))]
    [UpdateBefore(typeof(STUpdateOrder.UOMovementUpdate.FixMovement))]
    [UpdateAfter(typeof(DefStMvGravitySystem))]
    [UpdateAfter(typeof(DefStMvRunSystem))]
    public class DefStMvDodgeOnGroundSystem : ComponentSystem
    {
        [Inject] private Group m_Group;

        protected override void OnUpdate()
        {
            OnSimulationUpdate(Time.deltaTime);
        }

        private void OnSimulationUpdate(float delta)
        {
            using (var cmd = new EntityCommandBuffer(Allocator.Temp))
            {
                for (var i = 0; i != m_Group.Length; i++)
                {
                    var input        = m_Group.Inputs[i];
                    var stamina      = m_Group.Staminas[i];
                    var runDodge     = m_Group.RunDodgeComponents[i];
                    var dodgeSetting = m_Group.DodgeSettings[i];
                    var motor        = m_Group.Motors[i];
                    var entity       = m_Group.Entities[i];

                    var velocityData = m_Group.Velocities[i];

                    runDodge.Cooldown -= delta;

                    if (input.Dodge > 0.5f && motor.IsGrounded()
                                           && runDodge.Cooldown <= 0f
                                           && stamina.Value >= runDodge.StaminaUse)
                    {
                        var onSlopeAndGrounded = motor.IsGrounded() && motor.IsOnSlope();

                        // Get the gravity
                        var gravity = Physics.gravity;
                        if (EntityManager.HasComponent<DefStMvGravity>(entity))
                        {
                            var gravityComponent = EntityManager.GetComponentData<DefStMvGravity>(entity);
                            gravity = gravityComponent.Mode == DefStMvGravity.GravityMode.Custom
                                ? gravityComponent.Gravity
                                : gravity;
                        }

                        // The player shouldn't gain any UP velocity if he is on a stair/slope
                        // or else he will slow down
                        if (!onSlopeAndGrounded)
                        {
                            velocityData.Velocity.y =  0;
                            velocityData.Velocity   -= gravity * runDodge.VerticalBump;
                        }

                        var direction = motor.transform.TransformDirection(input.RunDirection);
                        var motorRot  = motor.transform.forward.normalized;
                        direction = Vector3.Lerp(motorRot,
                                               direction,
                                               1 - (motorRot.magnitude - direction.magnitude))
                                           .normalized;

                        //velocityData.Velocity += direction * dodgeSetting.AdditiveForce;
                        var oldY          = velocityData.Velocity.y;
                        var previousSpeed = velocityData.Velocity.ToGrid(1).magnitude;
                        velocityData.Velocity +=
                            direction * (velocityData.Velocity.ToGrid(1).magnitude + dodgeSetting.AdditiveForce);
                        velocityData.Velocity = Vector3.ClampMagnitude
                        (velocityData.Velocity.ToGrid(1),
                            Mathf.Min(previousSpeed + dodgeSetting.AdditiveForce, velocityData.Velocity.ToGrid(1).magnitude)
                        );
                        velocityData.Velocity.y = oldY;

                        var speed =
                            Mathf.Min(
                                Mathf.Max(velocityData.Velocity.ToGrid(1).magnitude, dodgeSetting.MinimumSpeed),
                                runDodge.MaximalSpeed
                            );

                        velocityData.Velocity   = velocityData.Velocity.normalized * speed;
                        velocityData.Velocity.y = oldY;

                        // The player shouldn't do a microjump while being on a stair/slope or
                        // or else he will slow down
                        if (!onSlopeAndGrounded)
                            motor.MoveBy(Vector3.up * motor.CharacterController.skinWidth); // Remove ground flag

                        stamina.Value -= runDodge.StaminaUse;

                        runDodge.Cooldown = 0.625f;
                        runDodge.Direction = direction;
                    }

                    if (runDodge.Cooldown > 0.25f && runDodge.Cooldown < 0.625f
                        && runDodge.Direction != Vector3.zero)
                    {
                        motor.MoveBy(runDodge.Direction * 2 * Time.deltaTime);
                    }

                    if (motor.IsGrounded() && runDodge.Cooldown > 0.25f) runDodge.Cooldown = 0.25f;

                    cmd.SetComponent(entity, stamina);
                    cmd.SetComponent(entity, runDodge);
                    cmd.SetComponent(entity, velocityData);
                }
                
                cmd.Playback(EntityManager);
            }
        }

        private struct Group
        {
            public ComponentDataArray<StCharacter>                    Characters;
            public ComponentDataArray<DefStMvDodgeOnGroundExecutable> ExecuteFlags;
            public ComponentDataArray<DefStMvInput>                   Inputs;
            public ComponentDataArray<DefStMvDodgeOnGround>           RunDodgeComponents;
            public ComponentDataArray<DefStMvDodge>                   DodgeSettings;
            public ComponentDataArray<DefStMvStamina>                 Staminas;
            public ComponentDataArray<DefStVelocity>                  Velocities;
            public ComponentArray<CharacterControllerMotor>           Motors;
            public EntityArray                                        Entities;

            public readonly int Length;
        }
    }
}