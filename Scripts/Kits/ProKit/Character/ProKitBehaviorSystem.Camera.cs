using Stormium.Core;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Data;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace package.stormium.def.Kits.ProKit
{
    public partial class ProKitBehaviorSystem
    {
        private void UpdateCamera()
        {
            Entities.ForEach((Transform transform, ref ProKitMovementState state, ref AimLookState aimLook, ref CameraModifierData camModifier, ref Relative<PlayerDescription> player) =>
            {
                camModifier.Position = transform.position + new Vector3(0.0f, 1.6f, 0.0f);

                var aim = new float3(-aimLook.Aim.y, aimLook.Aim.x, 0);
                if (EntityManager.HasComponent(player.Target, ComponentType.ReadWrite<GamePlayerLocalTag>()))
                {
                    var basicUserCommand = EntityManager.GetComponentData<GamePlayerUserCommand>(player.Target);
                    aim.x = basicUserCommand.Look.x;
                    aim.y = basicUserCommand.Look.y;
                }

                camModifier.Rotation = Quaternion.Euler(aim);
            });
        }
    }
}