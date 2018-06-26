using System.Runtime.InteropServices;
using package.guerro.shared;
using package.stormium.core;
using Unity.Entities;
using UnityEngine;

namespace package.stormium.def
{
    [UpdateAfter(typeof(STUpdateOrder.UOMovementUpdate.Loop)),
     UpdateBefore(typeof(STUpdateOrder.UOMovementUpdate.FixMovement))]
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
            for (var i = 0; i != m_Group.Length; i++)
            {
                var input  = m_Group.Inputs[i];
                var comp   = m_Group.Components[i];
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

                if (input.Jump <= 0.5f
                    || motor.IsGrounded()
                    || velocityData.Velocity.y > -(gravity.y) * comp.MaximalVerticalForce)
                {
                    comp.JumpState = 0;
                }

                if (input.Jump > 0.5f && comp.JumpState > 0)
                {
                    var toAdd = -(gravity.y * comp.VerticalForceDelta) * Time.deltaTime;
                    toAdd += velocityData.Velocity.y;
                    if (toAdd > -gravity.y * comp.MaximalVerticalForce)
                    {
                        toAdd          = -gravity.y * comp.MaximalVerticalForce;
                        comp.JumpState = 0;
                    }

                    velocityData.Velocity.y = toAdd;
                }

                if (input.Jump > 0.5f && motor.IsGrounded()
                                      && comp.JumpState == 0)
                {
                    velocityData.Velocity.y =  0;
                    velocityData.Velocity   -= gravity * comp.BaseVerticalForce;

                    comp.JumpState++;
                }

                m_Group.Components[i] = comp;
                m_Group.Velocities[i] = velocityData;
            }
        }

        private struct Group
        {
            public ComponentDataArray<StCharacter>          Characters;
            public ComponentDataArray<DefStMvInput>         Inputs;
            public ComponentDataArray<DefStMvJump>          Components;
            public ComponentDataArray<DefStMvJumpState>     States;
            public ComponentDataArray<DefStVelocity>        Velocities;
            public ComponentArray<CharacterControllerMotor> Motors;
            public EntityArray                              Entities;

            public int Length;
        }
    }
}