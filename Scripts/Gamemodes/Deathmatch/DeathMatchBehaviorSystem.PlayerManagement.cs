using Stormium.Core;
using StormiumTeam.GameBase;
using Unity.Entities;

namespace Stormium.Default.GameModes
{
    public partial class DeathMatchBehaviorSystem
    {
        public EntityQuery PlayerWithoutDmData;
        
        public ModelIdent DmPlayerModelId;

        public void Init_PlayerManagement()
        {
            PlayerWithoutDmData = GetEntityQuery
            (
                ComponentType.ReadWrite<GamePlayer>(),
                ComponentType.Exclude<DeathMatchPlayer>()
            );
        }

        public void ManageClients()
        {
            Entities.With(PlayerWithoutDmData).ForEach((Entity entity, ref GamePlayer player) =>
            {
                PostUpdateCommands.AddComponent(entity, new DeathMatchPlayer(default));
                if (!EntityManager.HasComponent<ServerCameraState>(entity))
                    PostUpdateCommands.AddComponent(entity, new ServerCameraState());
                if (!EntityManager.HasComponent<GamePlayerUserCommand>(entity))
                    PostUpdateCommands.AddComponent(entity, new GamePlayerUserCommand());
                if (!EntityManager.HasComponent<GamePlayerActionCommand>(entity))
                    PostUpdateCommands.AddBuffer<GamePlayerActionCommand>(entity);
            });
        }
    }
}