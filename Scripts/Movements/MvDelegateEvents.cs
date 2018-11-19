using System;
using Unity.Entities;

namespace package.stormium.def.Movements
{
    public static class MvDelegateEvents
    {
        public static event Action<Entity> OnCharacterDodge;
        public static event Action<Entity> OnCharacterJump;
        public static event Action<Entity> OnCharacterWalljump;
        public static event Action<Entity> OnCharacterLand;
        
        public static void InvokeCharacterDodge(Entity entity)
        {
            OnCharacterDodge?.Invoke(entity);
        }

        public static void InvokeCharacterJump(Entity entity)
        {
            OnCharacterJump?.Invoke(entity);
        }
        
        public static void InvokeCharacterWalljump(Entity entity)
        {
            OnCharacterWalljump?.Invoke(entity);
        }
        
        public static void InvokeCharacterLand(Entity entity)
        {
            OnCharacterLand?.Invoke(entity);
        }
    }
}