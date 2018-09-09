using Unity.Entities;

namespace package.stormium.def.Movements.Data
{
    public struct DefStDodgeClientInput : IComponentData
    {
        public float      Value01;
        public InputState State;

        public DefStDodgeClientInput(float value01)
        {
            Value01              = value01;
            State                = InputState.None;
        }
        
        public DefStDodgeClientInput(InputState state)
        {
            Value01              = state != InputState.None ? 1f : 0f;
            State                = state;
        }
    }
    
    public struct DefStDodgeInput : IComponentData
    {
        public float      Value01;
        public float      TimeBeforeResetState;
        public InputState State;

        public DefStDodgeInput(float value01, float timeBeforeResetState)
        {
            Value01              = value01;
            State                = InputState.None;
            TimeBeforeResetState = timeBeforeResetState;
        }
        
        public DefStDodgeInput(InputState state, float timeBeforeResetState)
        {
            Value01              = state != InputState.None ? 1f : 0f;
            State                = state;
            TimeBeforeResetState = timeBeforeResetState;
        }
    }
}