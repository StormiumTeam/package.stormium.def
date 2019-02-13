using package.stormiumteam.networking;
using package.stormiumteam.networking.runtime.lowlevel;
using Stormium.Core;
using StormiumShared.Core.Networking;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.AddressableAssets;

namespace Stormium.Default.GameModes
{
    public struct DeathMatchData : IComponentData
    {
        public int MaxFrag;
    }
    
    public partial class DeathMatchBehaviorSystem : GameModeSystem, ISnapshotSubscribe, ISnapshotManageForClient
    {
        private ComponentGroup m_SimulatedGameModes;
        private PatternResult m_SnapshotPattern;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();
            
            m_SnapshotPattern = World.GetOrCreateManager<NetPatternSystem>()
                                     .GetLocalBank()
                                     .Register($"000{nameof(DeathMatchBehaviorSystem)}.Snapshot");
        }

        protected override void OnStartRunning()
        {
            Init_PlayerManagement();
            Init_CharacterManagement();

            m_SimulatedGameModes = GetComponentGroup
            (
                ComponentType.Create<DeathMatchData>(),
                ComponentType.Create<EntityAuthority>()
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
                
                // Force client's cameras to be on their characters.
                ForceCharacterCamera();
            }
        }

        private void ForceCharacterCamera()
        {
            ForEach((Entity entity, ref DeathMatchPlayer client) =>
            {
                
            });
        }

        // Snapshot Implementation
        public PatternResult GetSystemPattern()
        {
            return m_SnapshotPattern;
        }

        public void SubscribeSystem()
        {
        }

        public DataBufferWriter WriteData(SnapshotReceiver receiver, StSnapshotRuntime runtime)
        {
            var buffer = new DataBufferWriter(0, Allocator.Temp);

            if (!EntityManager.HasComponent<DeathMatchPlayer>(receiver.Client))
                return buffer;

            var dmClient = EntityManager.GetComponentData<DeathMatchPlayer>(receiver.Client);
            var dmChar = dmClient.Character;
            
            return buffer;
        }

        public void ReadData(SnapshotSender sender, StSnapshotRuntime runtime, DataBufferReader sysData)
        {
        }
    }
}