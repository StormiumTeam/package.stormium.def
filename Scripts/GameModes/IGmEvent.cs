using package.stormiumteam.shared;

namespace package.stormium.def.GameModes
{
    public interface IGmEventOnMatchStarted : IAppEvent
    {
        void GameModeOnMatchStarted();
    }

    public interface IGmEventOnMatchEnded : IAppEvent
    {
        void GameModeOnMatchEnded();
    }

    public interface IGmEventOnMapLoaded : IAppEvent
    {
        void GameModeOnMapLoaded(GameMap map);
    }

    public interface IGmEventOnMapUnloaded : IAppEvent
    {
        void GameModeOnMapUnloaded(GameMap map);
    }

    public interface IGmEventProcedureSpawnPlayer : IAppEvent
    {
        void GameModeSpawnPlayer();
    }
}