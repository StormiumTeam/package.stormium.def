using Runtime.Data;
using Stormium.Default.States;
using Unity.Entities;
using UnityEngine;

namespace package.stormium.def.Kits.ProKit
{
    public partial class ProKitBehaviorSystem
    {
        private void UpdateCamera()
        {
            ForEach((Transform transform, ref ProKitBehaviorSettings data, ref AimLookState aimLook, ref CameraModifierData camModifier) =>
            {
                camModifier.Position = transform.position + new Vector3(0.0f, 1.6f, 0.0f);
                camModifier.Rotation = Quaternion.Euler(-aimLook.Aim.y, aimLook.Aim.x, 0);
            });
        }
    }
}