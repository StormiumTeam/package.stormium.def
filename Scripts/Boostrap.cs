using package.stormiumteam.shared.modding;
using Unity.Entities;
using UnityEngine;

namespace package.stormium.def
{
    public class Boostrap : CModBootstrap
    {
        protected override void OnRegister()
        {
            // Create systems
            Register<DefStMvRunInputSystem>();
            Register<DefStMvRunSystem>();
            Register<StEntityHeadLookAtSystem>();
            Register<StDefaultCharacterCameraSystem>();
        }

        protected override void OnUnregister()
        {
        }

        public void Register<T>()
            where T : ScriptBehaviourManager
        {
            World.Active.GetOrCreateManager<T>();
        }
    }
}