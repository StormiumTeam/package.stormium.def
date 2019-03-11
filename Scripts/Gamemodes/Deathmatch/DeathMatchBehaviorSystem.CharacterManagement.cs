using package.stormium.def;
using package.stormium.def.Kits.ProKit;
using package.StormiumTeam.GameBase;
using Stormium.Default.States;
using Stormium.Default.Tests;
using StormiumShared.Core.Networking;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Data;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Stormium.Default.GameModes
{
    public partial class DeathMatchBehaviorSystem
    {
        public ComponentGroup CreateCharacterForPlayerGroup;
        public ComponentGroup SpawnGroup;
        public ModelIdent     CharacterModel;

        public void Init_CharacterManagement()
        {
            CreateCharacterForPlayerGroup = GetComponentGroup
            (
                ComponentType.ReadWrite<DeathMatchPlayer>()
            );

            SpawnGroup = GetComponentGroup
            (
                ComponentType.ReadWrite<DeathMatchSpawn>()
            );

            CharacterModel = World.GetOrCreateManager<TestCharacterProvider>().GetModelIdent();
        }

        public void CreateCharacters()
        {
            const Allocator allocator = Allocator.TempJob;

            var length = CreateCharacterForPlayerGroup.CalculateLength();

            using (var entityArray = CreateCharacterForPlayerGroup.ToEntityArray(allocator))
            using (var playerArray = CreateCharacterForPlayerGroup.ToComponentDataArray<DeathMatchPlayer>(allocator))
                for (var i = 0; i != length; i++)
                {
                    var entity = entityArray[i];
                    var player = playerArray[i];

                    if (player.Character != default && EntityManager.Exists(player.Character))
                        continue;

                    SpawnCharacter(entity);
                }
        }

        public void DestroyCharacters()
        {
            /*ForEach((Entity entity, ref DeathMatchCharacter character) =>
            {
                if (character.Player != default && EntityManager.Exists(character.Player))
                    return;

                PostUpdateCommands.DestroyEntity(entity);
            });*/
        }

        public void ManageCharacters()
        {
            ForEach((Entity entity, Transform transform, ref DeathMatchCharacter character, ref HealthState healthState, ref Velocity velocity) =>
            {
                // Unspawn him
                if (healthState.Health <= 0 && character.NextRespawn == default)
                {
                    healthState.Health = 0;
                    velocity.Value = float3.zero;
                    character.NextRespawn = Tick + 2500;
                }

                if (character.NextRespawn != default && character.NextRespawn < Tick)
                {
                    character.NextRespawn = default;
                    healthState.Health = healthState.MaxHealth;
                    transform.position = SpawnGroup.GetComponentArray<DeathMatchSpawn>()[Random.Range(0, SpawnGroup.CalculateLength())].transform.position;
                }
            });
        }

        private void SpawnCharacter(Entity playerEntity)
        {
            var chrEntity = GameMgr.SpawnLocal(CharacterModel);

            EntityManager.SetComponentData(chrEntity, new ProKitMovementSettings
            {
                GroundSettings = SrtGroundSettings.NewBase(),
                AerialSettings = SrtAerialSettings.NewBase()
            });

            EntityManager.AddBuffer<ActionContainer>(chrEntity);
            EntityManager.AddComponentData(chrEntity, new DeathMatchCharacter(playerEntity));
            EntityManager.AddComponentData(chrEntity, new HealthState(100, 100));
            EntityManager.AddComponentData(chrEntity, new DestroyChainReaction(playerEntity));
            EntityManager.SetComponentData(playerEntity, new DeathMatchPlayer(chrEntity));
            
            EntityManager.ReplaceOwnerData(chrEntity, playerEntity);

            var rocketAction = World.GetExistingManager<ProRocketActionProvider>().SpawnLocal(chrEntity, 0);
            var detonateAction = World.GetExistingManager<ProRocketDetonateActionProvider>().SpawnLocal(chrEntity, playerEntity, 1);
            
            EntityManager.AddComponentData(rocketAction, new DestroyChainReaction(chrEntity));
            EntityManager.AddComponentData(detonateAction, new DestroyChainReaction(chrEntity));

            //var railgunAction = World.GetExistingManager<ProRailgunActionProvider>().SpawnLocal(chrEntity, new ProRailgunAction{ScanRadius = 0.1f}, 0);
            
            //EntityManager.AddComponentData(railgunAction, new DestroyChainReaction(chrEntity));

            // Force camera
            var camera = EntityManager.GetComponentData<ServerCameraState>(playerEntity);
            camera.Target = chrEntity;
            EntityManager.SetComponentData(playerEntity, camera);

            var transform = EntityManager.GetComponentObject<Transform>(chrEntity);

            transform.position = SpawnGroup.GetComponentArray<DeathMatchSpawn>()[Random.Range(0, SpawnGroup.CalculateLength())].transform.position;
        }
    }
}