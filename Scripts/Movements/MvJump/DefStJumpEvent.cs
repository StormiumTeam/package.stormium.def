using Unity.Entities;

namespace package.stormium.def.Movements
{
    public struct DefStJumpEvent : IComponentData
    {
        public float  Timestamp;
        public int    Frame;
        public Entity ServerTarget;

        public DefStJumpEvent(float timestamp, int frame, Entity serverTarget)
        {
            Timestamp    = timestamp;
            Frame        = frame;
            ServerTarget = serverTarget;
        }
    }

    public struct CmdMvJump : IComponentData
    {
        
    }
}