using Unity.Entities;

namespace Stormium.Default.GameModes
{
    public struct DeathMatchCharacter : IComponentData
    {
        public Entity Player;

        public DeathMatchCharacter(Entity player)
        {
            Player = player;
        }
    }
}