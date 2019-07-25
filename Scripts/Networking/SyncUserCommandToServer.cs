using StormiumTeam.GameBase;

namespace Scripts.Networking
{
    public class SyncUserCommandToServer : BaseComponentSystem
    {
        private GameManager m_GameManager;

        protected override void OnCreate()
        {
            m_GameManager = World.GetOrCreateSystem<GameManager>();
        }

        protected override void OnUpdate()
        {
            if (m_GameManager.GameType == GameType.Server)
                return;

            var serverMgr = m_GameManager.ServerManager;
        }
    }
}