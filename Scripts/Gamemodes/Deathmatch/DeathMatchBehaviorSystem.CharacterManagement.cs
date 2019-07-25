using package.stormium.def;
using package.stormium.def.Kits.ProKit;
using package.StormiumTeam.GameBase;
using Stormium.Default.Tests;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
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
        public EntityQuery CreateCharacterForPlayerGroup;
        public EntityQuery SpawnGroup;
        public ModelIdent     CharacterModel;

        public void Init_CharacterManagement()
        {
            CreateCharacterForPlayerGroup = GetEntityQuery
            (
                ComponentType.ReadWrite<DeathMatchPlayer>()
            );

            SpawnGroup = GetEntityQuery
            (
                ComponentType.ReadWrite<DeathMatchSpawn>()
            );

            CharacterModel = World.GetOrCreateSystem<TestCharacterProvider>().GetModelIdent();
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
            Entities.ForEach((Entity entity, Transform transform, ref DeathMatchCharacter character, ref LivableHealth health, ref Velocity velocity) =>
            {
                // Unspawn him
                if (health.Value <= 0 && character.NextRespawn == default)
                {
                    velocity.Value = float3.zero;
                    character.NextRespawn = GameTime.Tick + GameTime.Convert(2.5f);
                }

                if (character.NextRespawn != default && character.NextRespawn < GameTime.Tick)
                {
                    var tempSpawns = SpawnGroup.ToEntityArray(Allocator.TempJob);
                    var spawn = EntityManager.GetComponentObject<DeathMatchSpawn>(tempSpawns[Random.Range(0, tempSpawns.Length)]);
                    
                    tempSpawns.Dispose();
                    
                    character.NextRespawn = default;
                    transform.position = spawn.transform.position;

                    var healthEvent = PostUpdateCommands.CreateEntity();
                    
                    PostUpdateCommands.AddComponent(healthEvent, new ModifyHealthEvent(ModifyHealthType.SetMax, 0, entity));
                }
            });
        }

        private void SpawnCharacter(Entity playerEntity)
        {
            //var chrEntity = GameMgr.SpawnLocal(CharacterModel);
            var chrEntity = World.GetExistingSystem<TestCharacterProvider>().SpawnLocal();
            
            EntityManager.SetComponentData(chrEntity, new ProKitMovementSettings
            {
                GroundSettings = SrtGroundSettings.NewBase(),
                AerialSettings = SrtAerialSettings.NewBase()
            });

            EntityManager.AddBuffer<ActionContainer>(chrEntity);
            EntityManager.AddComponentData(chrEntity, new DeathMatchCharacter(playerEntity));
            EntityManager.AddComponentData(chrEntity, new DestroyChainReaction(playerEntity));
            EntityManager.SetComponentData(playerEntity, new DeathMatchPlayer(chrEntity));
            
            EntityManager.ReplaceOwnerData(chrEntity, playerEntity);

            using (var healthEntities = new NativeList<Entity>())
            {
                var data = new DefaultHealthData.CreateInstance
                {
                    value = 100,
                    max   = 100,
                    owner = chrEntity
                };
                World.GetExistingSystem<DefaultHealthData.InstanceProvider>().SpawnLocalEntityWithArguments(data, healthEntities);
            }

            // Force camera
            var camera = EntityManager.GetComponentData<ServerCameraState>(playerEntity);
            camera.Data.Target = chrEntity;
            EntityManager.SetComponentData(playerEntity, camera);

            var transform = EntityManager.GetComponentObject<Transform>(chrEntity);
            
            var tempSpawns = SpawnGroup.ToEntityArray(Allocator.TempJob);
            var spawn      = EntityManager.GetComponentObject<DeathMatchSpawn>(tempSpawns[Random.Range(0, tempSpawns.Length)]);
                    
            tempSpawns.Dispose();

            transform.position = spawn.transform.position;
        }
    }
}