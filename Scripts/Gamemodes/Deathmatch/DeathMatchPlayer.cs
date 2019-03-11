using StormiumShared.Core.Networking;
using Unity.Entities;

namespace Stormium.Default.GameModes
{
    public struct DeathMatchPlayer : IStateData, IComponentData
    {
        public Entity Character;

        public DeathMatchPlayer(Entity character)
        {
            Character = character;
        }
    }
}