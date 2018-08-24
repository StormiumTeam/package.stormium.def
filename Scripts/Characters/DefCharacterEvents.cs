using package.stormiumteam.shared;
using Unity.Entities;

namespace package.stormium.def.characters
{
    public class EventCharacterCreated
    {
        public struct Arguments : IDelayComponentArguments
        {
            public Entity Character;
            public Entity Parameters;
            
            public Arguments(Entity characterId, Entity parametersId)
            {
                Character = characterId;
                Parameters = parametersId;
            }
        }
        
        public interface IEv : IAppEvent
        {
            void OnGameCharacterCreated(Arguments args);
        }
    }
}