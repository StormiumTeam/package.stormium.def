using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Data;
using UnityEngine;

namespace package.stormium.def.Kits.ProKit
{
    public partial class ProKitBehaviorSystem
    {
        private void UpdateCamera()
        {
            ForEach((Transform transform, ref ProKitMovementState state, ref AimLookState aimLook, ref CameraModifierData camModifier, ref OwnerState<PlayerDescription> player) =>
            {
                camModifier.Position = transform.position + new Vector3(0.0f, 1.6f, 0.0f);
                camModifier.Rotation = Quaternion.Euler(-aimLook.Aim.y, aimLook.Aim.x, 0);
            });
        }
    }
}  