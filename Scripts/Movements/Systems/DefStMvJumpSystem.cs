using package.stormiumteam.shared;
using package.stormium.core;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace package.stormium.def
{
    public interface IDefStCharacterOnJump : IAppEvent
    {
        void CharacterOnJump(Entity entity);
    }
    
    [UpdateAfter(typeof(STUpdateOrder.UOMovementUpdate.Loop))]
    [UpdateBefore(typeof(STUpdateOrder.UOMovementUpdate.FixMovement))]
    [UpdateAfter(typeof(DefStMvGravitySystem))]
    [UpdateAfter(typeof(DefStMvRunSystem))]
    public class DefStMvJumpSystem : ComponentSystem
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
                    var input  = m_Group.Inputs[i];
                    var comp   = m_Group.Components[i];
                    var state  = m_Group.States[i];
                    var motor  = m_Group.Motors[i];
                    var entity = m_Group.Entities[i];

                    var velocityData = m_Group.Velocities[i];

                    var gravity = Physics.gravity;
                    if (EntityManager.HasComponent<DefStMvGravity>(entity))
                    {
                        var gravityComponent = EntityManager.GetComponentData<DefStMvGravity>(entity);
                        gravity = gravityComponent.Mode == DefStMvGravity.GravityMode.Custom
                            ? gravityComponent.Gravity
                            : gravity;
                    }

                    if (state.ActionStartTime < comp.MaxTimeBetweenJumps + 10)
                        state.ActionStartTime += delta;

                    var doJump = input.Jump > 0
                                 &&
                                 (
                                     motor.IsGrounded() ||
                                     (state.CurrentCombo < comp.MaximumConsecutiveAirJump
                                      && state.CurrentComboFromGround > 0
                                      && state.ActionStartTime > comp.MinTimeBetweenJumps
                                      && state.ActionStartTime < comp.MaxTimeBetweenJumps)
                                 );
                    /*if (input.Jump > 0
                        && motor.IsGrounded())
                    {
                        doJump = true;
                    }
                    else if (input.Jump > 0
                             && state.CurrentCombo < comp.MaximumConsecutiveAirJump
                             && state.ActionStartTime > comp.MinTimeBetweenJumps
                             && state.ActionStartTime < comp.MaxTimeBetweenJumps)
                    {
                        doJump = true;
                    }*/

                    if (motor.IsGrounded())
                    {
                        state.ActionStartTime        = 0;
                        state.CurrentCombo           = 0;
                        state.CurrentComboFromGround = 0;
                    }

                    if (doJump)
                    {
                        state.ActionStartTime = 0;

                        velocityData.Velocity.y =  Mathf.Max(0, velocityData.Velocity.y);
                        velocityData.Velocity   += -gravity * comp.GravityComplementForce * comp.BaseVerticalForce;

                        // Apply a little dash in the current direction
                        if (!motor.IsGrounded())
                        {
                            var direction = motor.transform.rotation * ((Vector3) input.RunDirection).normalized;

                            // We don't want the player to instantly be in this direction
                            direction = Vector3.Lerp(velocityData.Velocity.ToGrid(1).normalized, direction, 0.99f);

                            var previousY         = velocityData.Velocity.y;
                            var previousSpeed     = velocityData.Velocity.ToGrid(1).magnitude;
                            var predictedVelocity = velocityData.Velocity + direction.normalized;
                            velocityData.Velocity += direction * (velocityData.Velocity.ToGrid(1).magnitude + 2);
                            velocityData.Velocity = Vector3.ClampMagnitude
                            (velocityData.Velocity.ToGrid(1),
                                Mathf.Min(previousSpeed + 1, velocityData.Velocity.ToGrid(1).magnitude)
                            );
                            velocityData.Velocity   = Vector3.Lerp(velocityData.Velocity, predictedVelocity, 0.1f);
                            velocityData.Velocity.y = previousY;

                            state.CurrentCombo++;
                        }
                        else
                        {
                            state.CurrentComboFromGround++;
                        }
                        
                        foreach (var manager in AppEvent<IDefStCharacterOnJump>.eventList)
                        {
                            AppEvent<IDefStCharacterOnJump>.Caller = this;
                            manager.CharacterOnJump(entity);
                        }
                    }

                    input.Jump = 0;

                    cmd.SetComponent(entity, input);
                    cmd.SetComponent(entity, state);
                    cmd.SetComponent(entity, velocityData);
                }
                
                cmd.Playback(EntityManager);
            }
        }

        private struct Group
        {
            public ComponentDataArray<StCharacter>           Characters;
            public ComponentDataArray<DefStMvJumpExecutable> ExecuteFlags;
            public ComponentDataArray<DefStMvInput>          Inputs;
            public ComponentDataArray<DefStMvJump>           Components;
            public ComponentDataArray<DefStMvJumpState>      States;
            public ComponentDataArray<DefStVelocity>         Velocities;
            public ComponentArray<CharacterControllerMotor>  Motors;
            public EntityArray                               Entities;

            public readonly int Length;
        }
    }
}