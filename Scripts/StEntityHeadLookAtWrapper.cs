using System;
using package.stormiumteam.shared;
using Unity.Entities;
using UnityEngine;

namespace package.stormium.def
{
    [Serializable]
    public struct StEntityHeadLookAt : IComponentData
    {
        public Vector3 Position;
        public Quaternion Rotation;
    }

    [Serializable]
    public struct NetStEntityHeadLookAt : IComponentData
    {
        public Vector3 Position;
        public Quaternion Rotation;
    }

    public class StEntityHeadLookAtWrapper : BetterComponentWrapper<StEntityHeadLookAt>
    {
        public StEntityHeadLookAtWrapper()
        {
            Value = new StEntityHeadLookAt
            {
                Position  = new Vector3(0, 0.6f, 0),
                Rotation = Quaternion.identity,
            };
        }

        protected override void OnUnityAwake()
        {
            // Also add the net version of the component
            AddComponentData(new NetStEntityHeadLookAt());
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(Value.Position, Value.Rotation.eulerAngles.normalized);
            //Gizmos.color = Color.green;
            //Gizmos.DrawRay(Value.Position, new Vector3(0, Value.RotationY, 0));
            Gizmos.color = Color.black;
            Gizmos.DrawRay(Value.Position, Vector3.up);
        }
    }
}