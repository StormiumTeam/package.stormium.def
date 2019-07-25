using package.stormiumteam.networking.runtime.lowlevel;
using Stormium.Core;
using StormiumTeam.GameBase;
using Unity.Collections;
using Unity.Entities;

namespace Stormium.Default.GameModes
{
    public struct DeathMatchData : IComponentData
    {
        public int MaxFrag;
    }
    
    public partial class DeathMatchBehaviorSystem : GameModeSystem
    {
        private EntityQuery m_SimulatedGameModes;

        protected override void OnCreate()
        {
            base.OnCreate();
            
            Init_PlayerManagement();
            Init_CharacterManagement();

            m_SimulatedGameModes = GetEntityQuery
            (
                ComponentType.ReadWrite<DeathMatchData>(),
                ComponentType.ReadWrite<EntityAuthority>()
            );
        }

        protected override void OnUpdate()
        {
            const Allocator allocator = Allocator.TempJob;
            
            using (var entityArray = m_SimulatedGameModes.ToEntityArray(allocator))
            using (var dataArray = m_SimulatedGameModes.ToComponentDataArray<DeathMatchData>(allocator))
            {
                if (entityArray.Length == 0)
                    return;
                
                // Some what internal...
                ManageClients();
                CreateCharacters();
                DestroyCharacters();

                ManageCharacters();
                ManageEvents();
                
                // Force client's cameras to be on their characters.
                ForceCharacterCamera();
            }
        }

        private void ForceCharacterCamera()
        {
            Entities.ForEach((Entity entity, ref DeathMatchPlayer client) =>
            {
                
            });
        }
    }
}