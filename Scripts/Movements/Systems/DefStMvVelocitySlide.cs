using package.guerro.shared;
using Unity.Entities;

namespace package.stormium.def
{
    public class DefStMvVelocitySlide : ComponentSystem
    {
        private struct Group
        {
            public ComponentDataArray<StCharacter>          Characters;
            public ComponentDataArray<DefStVelocity>        Velocities;
            public ComponentArray<CharacterControllerMotor> Motors;

            public int Length;
        }
        
        protected override void OnUpdate()
        {
            //throw new System.NotImplementedException();
        }
    }
}