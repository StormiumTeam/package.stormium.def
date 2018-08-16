using package.stormiumteam.shared;
using Unity.Entities;

namespace package.stormium.def
{
    public class DefStMvVelocitySlide : ComponentSystem
    {
        protected override void OnUpdate()
        {
            //throw new System.NotImplementedException();
        }

        private struct Group
        {
            public ComponentDataArray<StCharacter>          Characters;
            public ComponentDataArray<DefStVelocity>        Velocities;
            public ComponentArray<CharacterControllerMotor> Motors;

            public readonly int Length;
        }
    }
}