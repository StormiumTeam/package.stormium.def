using System;
using System.Collections.Generic;
using StandardAssets.Characters.Physics;
using Stormium.Core;
using Stormium.Default.Kits.ProKit;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Data;
using Unity.Entities;
using UnityEngine;

namespace package.stormium.def.Kits.ProKit
{
    [UpdateInGroup(typeof(ProCharacterSystemGroup))]
    [AlwaysUpdateSystem]
    public partial class ProKitBehaviorSystem : GameBaseSystem
    {
        private ComponentGroup m_CharacterMovementGroup;
        private PhysicQueryManager m_QueryManager;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_CharacterMovementGroup = GetComponentGroup
            (
                ComponentType.ReadWrite<ProKitMovementSettings>(),
                ComponentType.ReadWrite<ProKitMovementState>(),
                ComponentType.ReadWrite<ProKitInputState>(),
                ComponentType.ReadWrite<AimLookState>(),
                ComponentType.ReadWrite<Velocity>(),
                ComponentType.ReadWrite<OpenCharacterController>(),
                ComponentType.ReadWrite<EntityAuthority>(),
                ComponentType.Exclude<DeactivateMovement>()
            );

            m_Collisions = new List<CollisionInfo>();
            m_QueryManager = World.GetExistingManager<PhysicQueryManager>();
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
                if (!EntityManager.HasComponent<OwnerState<PlayerDescription>>(entity))
                    return;

                var owner = EntityManager.GetComponentData<OwnerState<PlayerDescription>>(entity).Target;
                if (!EntityManager.HasComponent<BasicUserCommand>(owner))
                    return;

                var commands = EntityManager.GetComponentData<BasicUserCommand>(owner);

                inputState.Movement = commands.Move;
                inputState.QueueJump = Convert.ToByte(commands.Jump);
                inputState.QueueDodge = Convert.ToByte(commands.Dodge);
                aimLook.Aim         = commands.Look;

                if (commands.TapJumpFrame == Time.frameCount) inputState.QueueJump = 2;
                if (commands.TapDodgeFrame == Time.frameCount) inputState.QueueDodge = 2;
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