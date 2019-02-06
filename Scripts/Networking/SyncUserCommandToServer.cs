using Runtime;
using Unity.Entities;

namespace Scripts.Networking
{
    public class SyncUserCommandToServer : ComponentSystem
    {
        private StormiumGameManager m_GameManager;

        protected override void OnCreateManager()
        {
            m_GameManager = World.GetOrCreateManager<StormiumGameManager>();
        }

        protected override void OnUpdate()
        {
            if (m_GameManager.GameType == GameType.Server)
                return;

            var serverMgr = m_GameManager.ServerManager;
        }
    }
}