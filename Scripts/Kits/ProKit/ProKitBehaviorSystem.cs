using System.Collections.Generic;
using StandardAssets.Characters.Physics;
using Stormium.Core;
using Stormium.Default.States;
using StormiumShared.Core.Networking;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace package.stormium.def.Kits.ProKit
{
    [AlwaysUpdateSystem]
    public partial class ProKitBehaviorSystem : ComponentSystem
    {
        private ComponentGroup m_CharacterMovementGroup;

        protected override void OnCreateManager()
        {
            m_CharacterMovementGroup = GetComponentGroup
            (
                ComponentType.Create<ProKitBehaviorSettings>(),
                ComponentType.Create<ProKitInputState>(),
                ComponentType.Create<AimLookState>(),
                ComponentType.Create<Velocity>(),
                ComponentType.Create<OpenCharacterController>(),
                ComponentType.Create<SimulateEntity>()
            );
            
            m_Collisions = new List<CollisionInfo>();
        }

        protected override void OnUpdate()
        {
            UpdateInputsFromOwners();
            
            // Simulate rotation from AimLook component
            SimulateRotations();
            // Simulate standard SRT movements (Run, Dodge, Jump, etc...)
            SimulateMovements();

            UpdateCamera();
        }

        private void UpdateInputsFromOwners()
        {
            ForEach((Entity entity, ref ProKitInputState inputState, ref AimLookState aimLook) =>
            {
                if (!EntityManager.HasComponent<OwnerState>(entity))
                    return;

                var owner = EntityManager.GetComponentData<OwnerState>(entity).Target;
                if (!EntityManager.HasComponent<BasicUserCommand>(owner))
                    return;

                var commands = EntityManager.GetComponentData<BasicUserCommand>(owner);

                inputState.Movement = commands.Move;
                aimLook.Aim         = commands.Look;

            });
        }

        private void SimulateRotations()
        {
            ForEach((OpenCharacterController controller, ref AimLookState aimLook) =>
            {
                controller.transform.rotation = Quaternion.Euler(0, aimLook.Aim.x, 0);
            }, m_CharacterMovementGroup);
        }
    }
}