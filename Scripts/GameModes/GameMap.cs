using Unity.Entities;
using UnityEngine.SceneManagement;

namespace package.stormium.def.GameModes
{
    public struct GameMap
    {
        
    }
    
    public class GameMapManager : ComponentSystem
    {
        protected override void OnUpdate()
        {
            
        }

        public void RegisterMap(string packId)
        {
            
        }

        public void LoadMap(string packId)
        {
            if (packId == "arenatest")
            {
                SceneManager.LoadScene("Scenes/arenatest", LoadSceneMode.Single);
            }
        }
    }
}