using package.guerro.shared;
using package.stormium.core;
using Unity.Entities;
using UnityEngine;

namespace package.stormium.def
{
    [UpdateAfter(typeof(STUpdateOrder.UOMovementUpdate.PreInit)),
     UpdateBefore(typeof(STUpdateOrder.UOMovementUpdate.Loop))]
    public class DefStMvGravitySystem : ComponentSystem
    {
        [Inject] private Group m_Group;


        protected override void OnStartRunning()
        {
            UpdateRigidbodySystem.OnBeforeSimulateItem += OnSimulationUpdate;
        }

        private void OnSimulationUpdate(float delta)
        {
            for (var i = 0; i != m_Group.Length; i++)
            {
                var entity = m_Group.EntityArray[i];
                var comp  = m_Group.Components[i];
                var motor = m_Group.Motors[i];

                var velocityData = m_Group.Velocities[i];

                if (!motor.IsGrounded())
                    velocityData.Velocity += (comp.Mode == DefStMvGravity.GravityMode.Physics
                                                 ? Physics.gravity
                                                 : comp.Gravity) * delta;
                else if (velocityData.Velocity.y <= 0.0001f)
                    velocityData.Velocity.y = -motor.CharacterController.stepOffset;

                if (motor.IsGrounded())
                    motor.CharacterController.stepOffset = 0.3f;
                else
                    motor.CharacterController.stepOffset = 0.1f;

                if (EntityManager.HasComponent<CameraTargetData>(entity))
                {
                    var targetData = EntityManager.GetComponentData<CameraTargetData>(entity);
                    //targetData.PositionOffset += 
                }
                
                m_Group.Velocities[i] = velocityData;
            }
        }

        protected override void OnUpdate()
        {
        }

        private struct Group
        {
            public ComponentDataArray<StCharacter>          Characters;
            public ComponentDataArray<DefStMvGravity>       Components;
            public ComponentDataArray<DefStVelocity>        Velocities;
            public ComponentArray<CharacterControllerMotor> Motors;
            public EntityArray EntityArray;

            public int Length;
        }
    }
}