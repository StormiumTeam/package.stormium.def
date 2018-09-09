using Unity.Entities;

namespace package.stormium.def.Movements
{
    public struct DefStDodgeEvent : IComponentData
    {
        public float Timestamp;
        public int Frame;
        public Entity ServerTarget;

        public DefStDodgeEvent(float timestamp, int frame, Entity serverTarget)
        {
            Timestamp = timestamp;
            Frame = frame;
            ServerTarget = serverTarget;
        }
    }
}