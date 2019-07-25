using Unity.Entities;

namespace Stormium.Default.GameModes
{
    public struct DeathMatchPlayer : IComponentData
    {
        public Entity Character;

        public DeathMatchPlayer(Entity character)
        {
            Character = character;
        }
    }
}