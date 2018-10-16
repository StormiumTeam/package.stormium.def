using package.stormiumteam.shared;
using Unity.Entities;

namespace package.stormium.def
{
    public interface IOnBodyBumpedEvent : IAppEvent
    {
        void OnBodyBumpedEvent(Entity body, Entity bumper);
    }
}