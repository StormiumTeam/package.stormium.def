using Unity.Entities;

namespace package.stormium.def
{
    public interface IGameModeComponent
    {
             
    }
         
    public struct GameModeTag : IComponentData
    {
             
    }
         
    public abstract class GameModeManager : GameComponentSystem
    {
        public abstract Entity CreateInstance();
        public abstract void   RemoveInstance(Entity target);
             
        protected override void OnUpdate()
        {
                 
        }
    }
     
    public abstract class SubGameModeSystem : GameComponentSystem
    {
             
    }
}