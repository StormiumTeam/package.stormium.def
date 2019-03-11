using Unity.Entities;

namespace Stormium.Default.GameModes
{
    public struct DeathMatchCharacter : IComponentData
    {
        public Entity Player;
        public int NextRespawn;
        
        public DeathMatchCharacter(Entity player)
        {
            Player = player;
            NextRespawn = default;
        }
    }
}