using package.stormiumteam.shared;
using package.stormium.core;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace package.stormium.def
{
    [UpdateAfter(typeof(STUpdateOrder.UOMovementUpdate.PreInit))]
    [UpdateBefore(typeof(STUpdateOrder.UOMovementUpdate.Loop))]
    public class DefStMvGravitySystem : ComponentSystem
    {
        [Inject] private Group m_Group;


        protected override void OnStartRunning()
        {
        }

        private void OnSimulationUpdate(float delta)
        {
            using (var cmd = new EntityCommandBuffer(Allocator.Temp))
            {
                for (var i = 0; i != m_Group.Length; i++)
                {
                    var entity = m_Group.EntityArray[i];
                    var comp   = m_Group.Components[i];
                    var motor  = m_Group.Motors[i];

                    var velocityData = m_Group.Velocities[i];

                    if (!motor.IsGrounded())
                        velocityData.Velocity += (comp.Mode == DefStMvGravity.GravityMode.Physics
                                                     ? Physics.gravity
                                                     : comp.Gravity) * delta;
                    else if (velocityData.Velocity.y <= 0.0001f)
                        velocityData.Velocity.y = -motor.CharacterController.stepOffset;

                    if (motor.IsGrounded())
                        motor.CharacterController.stepOffset = 0.4f;
                    else
                        motor.CharacterController.stepOffset = 0.2f;

                    if (EntityManager.HasComponent<oldCameraTargetData>(entity))
                    {
                        var targetData = EntityManager.GetComponentData<oldCameraTargetData>(entity);
                        //targetData.PositionOffset += 
                    }

                    cmd.SetComponent(entity, velocityData);
                }
                
                cmd.Playback(EntityManager);
            }
        }

        protected override void OnUpdate()
        {
            OnSimulationUpdate(Time.deltaTime);
        }

        private struct Group
        {
            public ComponentDataArray<StCharacter>          Characters;
            public ComponentDataArray<DefStMvGravity>       Components;
            public ComponentDataArray<DefStVelocity>        Velocities;
            public ComponentArray<CharacterControllerMotor> Motors;
            public EntityArray                              EntityArray;

            public readonly int Length;
        }
    }
}