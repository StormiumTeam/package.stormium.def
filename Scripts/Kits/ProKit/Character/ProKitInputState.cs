using Unity.Entities;
using Unity.Mathematics;

namespace package.stormium.def.Kits.ProKit
{
    public struct ProKitInputState : IComponentData
    {
        public float2 Movement;
        public byte   QueueJump;
        public byte   QueueDodge;

        public ProKitInputState(float2 movement, bool queueJump, bool queueDodge)
        {
            Movement   = movement;
            QueueJump  = queueJump ? (byte) 1 : (byte) 0;
            QueueDodge = queueDodge ? (byte) 1 : (byte) 0;
        }
    }
}