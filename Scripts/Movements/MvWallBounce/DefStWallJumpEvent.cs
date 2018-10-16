using Unity.Entities;
using UnityEngine;

namespace Scripts.Movements.MvWallBounce
{
    public struct DefStWallJumpEvent : IComponentData
    {
        public float   Timestamp;
        public int     Frame;
        public Vector3 PrevVelocity;
        public Vector3 Direction;
        public Entity  ServerTarget;

        public DefStWallJumpEvent(float timestamp, int frame, Entity serverTarget, Vector3 prevVelocity, Vector3 direction)
        {
            Timestamp    = timestamp;
            Frame        = frame;
            ServerTarget = serverTarget;
            PrevVelocity = prevVelocity;
            Direction    = direction;
        }
    }
}