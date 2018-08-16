using System;
using package.stormiumteam.shared;
using Unity.Entities;
using UnityEngine;

namespace package.stormium.def
{
    [Serializable]
    public struct DefStCharacterHead : IComponentData
    {
        public Vector3 Position;
        public Vector3 LookAt;
        public float   RotationY;
    }

    public class DefStCharacterHeadWrapper : BetterComponentWrapper<DefStCharacterHead>
    {
        public DefStCharacterHeadWrapper()
        {
            Value = new DefStCharacterHead
            {
                Position  = new Vector3(0, 0.6f, 0),
                LookAt    = new Vector3(),
                RotationY = 0f
            };
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(Value.Position, Value.LookAt);
            Gizmos.color = Color.green;
            Gizmos.DrawRay(Value.Position, new Vector3(0, Value.RotationY, 0));
            Gizmos.color = Color.black;
            Gizmos.DrawRay(Value.Position, Vector3.up);
        }
    }
}