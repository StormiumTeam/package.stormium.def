using Runtime.Data;
using Stormium.Core;
using Stormium.Default.States;
using Stormium.Default.Tests;
using StormiumShared.Core.Networking;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

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
                ComponentType.Create<StGamePlayer>(),
                ComponentType.Subtractive<DeathMatchPlayer>()
            );

            DmPlayerModelId = World.GetOrCreateManager<EntityModelManager>().Register("dm.player.model", SpawnDmPlayer, DestroyDmPlayer);
        }

        public void ManageClients()
        {
            ForEach((Entity entity, ref StGamePlayer player) =>
            {
                if (entity.Index == 0)
                    Debug.Log(entity);
                
                PostUpdateCommands.AddComponent(entity, new DeathMatchPlayer(default));
                if (!EntityManager.HasComponent<ServerCameraState>(entity))
                    PostUpdateCommands.AddComponent(entity, new ServerCameraState());
                if (!EntityManager.HasComponent<BasicUserCommand>(entity))
                    PostUpdateCommands.AddComponent(entity, new BasicUserCommand());
                if (!EntityManager.HasComponent<ActionUserCommand>(entity))
                    PostUpdateCommands.AddBuffer<ActionUserCommand>(entity);

            }, PlayerWithoutDmData);
        }

        private Entity SpawnDmPlayer(Entity origin, StSnapshotRuntime snapshotruntime)
        {
            return default;
        }
        
        private void DestroyDmPlayer(Entity worldentity)
        {
            return;
        }
    }
}