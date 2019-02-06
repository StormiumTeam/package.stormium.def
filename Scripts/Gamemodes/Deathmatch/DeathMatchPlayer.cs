using package.stormiumteam.networking;
using package.stormiumteam.networking.runtime.lowlevel;
using StormiumShared.Core.Networking;
using Unity.Entities;
using Unity.Jobs;

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