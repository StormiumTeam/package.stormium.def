using package.stormium.def.Network;
using package.stormiumteam.networking;
using package.stormiumteam.shared;
using Unity.Entities;

namespace package.stormium.def
{
    public abstract class GameComponentSystem : ComponentSystem
    {
        [Inject] protected MsgIdRegisterSystem MsgIdRegisterSystem;
        [Inject] protected GameServerManagement GameServerManagement;
        [Inject] protected AppEventSystem AppEventSystem;
        
        protected override void OnCreateManager(int capacity)
        {
            MsgIdRegisterSystem.Register(this);
            AppEventSystem.SubscribeToAll(this);
        }
    }
}