using StormiumTeam.GameBase;
using Unity.Entities;

namespace Stormium.Default.GameModes
{
    public struct DeathMatchCharacter : IComponentData
    {
        public Entity Player;
        public UTick NextRespawn;
        
        public DeathMatchCharacter(Entity player)
        {
            Player = player;
            NextRespawn = default;
        }
    }
}