using System;
using System.Collections.Generic;
using StandardAssets.Characters.Physics;
using Stormium.Core;
using Stormium.Default.Kits.ProKit;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Data;
using Unity.Entities;
using UnityEngine;

namespace package.stormium.def.Kits.ProKit
{
    [UpdateInGroup(typeof(ProCharacterSystemGroup))]
    [AlwaysUpdateSystem]
    public partial class ProKitBehaviorSystem : GameBaseSystem
    {
        private EntityQuery m_CharacterMovementAuthorityGroup;
        private EntityQuery m_CharacterMovementPredictionGroup;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_CharacterMovementAuthorityGroup = GetEntityQuery
            (
                ComponentType.ReadWrite<ProKitMovementSettings>(),
                ComponentType.ReadWrite<ProKitMovementState>(),
                ComponentType.ReadWrite<ProKitInputState>(),
                ComponentType.ReadWrite<AimLookState>(),
                ComponentType.ReadWrite<Velocity>(),
                ComponentType.ReadWrite<OpenCharacterController>(),
                ComponentType.ReadWrite<EntityAuthority>(),
                ComponentType.ReadWrite<LivableHealth>(),
                ComponentType.Exclude<DeactivateMovement>()
            );
            
            m_CharacterMovementPredictionGroup = GetEntityQuery
            (
                ComponentType.ReadWrite<ProKitMovementSettings>(),
                ComponentType.ReadWrite<ProKitMovementState>(),
                ComponentType.ReadWrite<ProKitInputState>(),
                ComponentType.ReadWrite<AimLookState>(),
                ComponentType.ReadWrite<Velocity>(),
                ComponentType.ReadWrite<OpenCharacterController>(),
                ComponentType.Exclude<DeactivateMovement>()
            );
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
            Entities.ForEach((Entity entity, ref ProKitInputState inputState, ref AimLookState aimLook) =>
            {
                if (!EntityManager.HasComponent<Relative<PlayerDescription>>(entity))
                    return;

                var owner = EntityManager.GetComponentData<Relative<PlayerDescription>>(entity).Target;
                if (!EntityManager.HasComponent<GamePlayerUserCommand>(owner))
                    return;

                var commands = EntityManager.GetComponentData<GamePlayerUserCommand>(owner);

                inputState.Movement = commands.Move;
                inputState.QueueJump = Convert.ToByte(commands.QueueJump);
                inputState.QueueDodge = Convert.ToByte(commands.QueueDodge);
                aimLook.Aim         = commands.Look;

                if (commands.IsJumping) inputState.QueueJump = 2;
                if (commands.IsDodging) inputState.QueueDodge = 2;
            });
        }

        private void SimulateRotations()
        {
            Entities.With(m_CharacterMovementAuthorityGroup).ForEach((OpenCharacterController controller, ref AimLookState aimLook) =>
            {
                controller.transform.rotation = Quaternion.Euler(0, aimLook.Aim.x, 0);
            });
        }
    }
}