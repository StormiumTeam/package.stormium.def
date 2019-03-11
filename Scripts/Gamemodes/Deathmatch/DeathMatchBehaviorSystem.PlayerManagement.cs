using Stormium.Core;
using Stormium.Default.States;
using StormiumShared.Core.Networking;
using StormiumTeam.GameBase;
using Unity.Entities;

namespace Stormium.Default.GameModes
{
    public partial class DeathMatchBehaviorSystem
    {
        public ComponentGroup PlayerWithoutDmData;
        
        public ModelIdent DmPlayerModelId;

        public void Init_PlayerManagement()
        {
            PlayerWithoutDmData = GetComponentGroup
            (
                ComponentType.ReadWrite<GamePlayer>(),
                ComponentType.Exclude<DeathMatchPlayer>()
            );

            DmPlayerModelId = World.GetExistingManager<EntityModelManager>().Register("dm.player.model", SpawnDmPlayer, DestroyDmPlayer);
        }

        public void ManageClients()
        {
            ForEach((Entity entity, ref GamePlayer player) =>
            {
                PostUpdateCommands.AddComponent(entity, new DeathMatchPlayer(default));
                if (!EntityManager.HasComponent<ServerCameraState>(entity))
                    PostUpdateCommands.AddComponent(entity, new ServerCameraState());
                if (!EntityManager.HasComponent<BasicUserCommand>(entity))
                    PostUpdateCommands.AddComponent(entity, new BasicUserCommand());
                if (!EntityManager.HasComponent<ActionUserCommand>(entity))
                    PostUpdateCommands.AddBuffer<ActionUserCommand>(entity);

            }, PlayerWithoutDmData);
        }

        private Entity SpawnDmPlayer(Entity origin, SnapshotRuntime runtime)
        {
            return default;
        }
        
        private void DestroyDmPlayer(Entity worldentity)
        {
            return;
        }
    }
}