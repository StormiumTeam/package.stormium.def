using System;
using StandardAssets.Characters.Physics;
using Stormium.Core;
using Stormium.Default.Kits.ProKit;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Data;
using StormiumTeam.Shared.Gen;
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

        private EntityQuery m_InputFromPlayerQuery;

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

            m_InputFromPlayerQuery = GetEntityQuery(typeof(ProKitInputState), typeof(AimLookState), typeof(Relative<PlayerDescription>));
        }

        protected override void OnUpdate()
        {
            // TODO....
            return;
            
            UpdateInputsFromOwners();

            // Simulate rotation from AimLook component
            SimulateRotations();
            // Simulate standard SRT movements (Run, Dodge, Jump, etc...)
            SimulateMovements();

            UpdateCamera();
        }

        private void UpdateInputsFromOwners()
        {
            ProKitInputState            inputState     = default;
            AimLookState                aimLook        = default;
            Relative<PlayerDescription> playerRelative = default;

            foreach (var _ in this.ToEnumerator_DDD(m_InputFromPlayerQuery, ref inputState, ref aimLook, ref playerRelative))
            {
                if (!EntityManager.HasComponent<GamePlayerUserCommand>(playerRelative.Target))
                    return;

                var commands = EntityManager.GetComponentData<GamePlayerUserCommand>(playerRelative.Target);

                inputState.Movement   = commands.Move;
                inputState.QueueJump  = Convert.ToByte(commands.QueueJump);
                inputState.QueueDodge = Convert.ToByte(commands.QueueDodge);
                aimLook.Aim           = commands.Look;

                if (commands.IsJumping) inputState.QueueJump  = 2;
                if (commands.IsDodging) inputState.QueueDodge = 2;
            }
        }

        private void SimulateRotations()
        {
            OpenCharacterController openCharacterController = default;
            AimLookState            aimLook                 = default;

            foreach (var _ in this.ToEnumerator_CD(m_CharacterMovementAuthorityGroup, ref openCharacterController, ref aimLook))
            {
                openCharacterController.transform.rotation = Quaternion.Euler(0, aimLook.Aim.x, 0);
            }
        }
    }
}