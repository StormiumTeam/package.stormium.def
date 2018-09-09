using Unity.Entities;

namespace package.stormium.def.Movements.Data
{
    public struct DefStJumpClientInput : IComponentData
    {
        public float      Value01;
        public InputState State;

        public DefStJumpClientInput(float value01)
        {
            Value01              = value01;
            State                = InputState.None;
        }
        
        public DefStJumpClientInput(InputState state)
        {
            Value01              = state != InputState.None ? 1f : 0f;
            State                = state;
        }
    }
    
    public struct DefStJumpInput : IComponentData
    {
        public float Value01;
        public float TimeBeforeResetState;
        public InputState State;

        public DefStJumpInput(float value01, float timeBeforeResetState)
        {
            Value01 = value01;
            State = InputState.None;
            TimeBeforeResetState = timeBeforeResetState;
        }
        
        public DefStJumpInput(InputState state, float timeBeforeResetState)
        {
            Value01 = state != InputState.None ? 1f : 0f;
            State   = state;
            TimeBeforeResetState = timeBeforeResetState;
        }
    }
}