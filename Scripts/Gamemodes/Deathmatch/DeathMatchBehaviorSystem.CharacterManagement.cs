using package.stormium.def;
using package.stormium.def.Kits.ProKit;
using Runtime;
using Stormium.Core;
using Stormium.Default.States;
using Stormium.Default.Tests;
using StormiumShared.Core.Networking;
using Unity.Collections;
using Unity.Entities;

namespace Stormium.Default.GameModes
{
    public partial class DeathMatchBehaviorSystem
    {
        public ComponentGroup CreateCharacterForPlayerGroup;
        public ModelIdent CharacterModel;
        
        public void Init_CharacterManagement()
        {
            CreateCharacterForPlayerGroup = GetComponentGroup
            (
                ComponentType.Create<DeathMatchPlayer>()
            );

            CharacterModel = World.GetExistingManager<TestCharacterProvider>().GetModelIdent();
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
            ForEach((Entity entity, ref DeathMatchCharacter character) =>
            {
                if (character.Player != default && EntityManager.Exists(character.Player))
                    return;
                
                PostUpdateCommands.DestroyEntity(entity);
            });
        }

        private void SpawnCharacter(Entity playerEntity)
        {
            var gameMgr = World.GetExistingManager<StormiumGameManager>();
            var chrEntity = gameMgr.SpawnLocal(CharacterModel);
            
            EntityManager.SetComponentData(chrEntity, new ProKitBehaviorSettings
            {
                GroundSettings = SrtGroundSettings.NewBase(),
                AerialSettings = SrtAerialSettings.NewBase()
            });
            
            EntityManager.AddComponentData(chrEntity, new DeathMatchCharacter(playerEntity));
            EntityManager.AddComponentData(chrEntity, new OwnerState {Target = playerEntity});
            EntityManager.AddComponentData(chrEntity, new GenerateEntitySnapshot());
            EntityManager.AddComponentData(chrEntity, new SimulateEntity());
            EntityManager.SetComponentData(playerEntity, new DeathMatchPlayer(chrEntity));
            
            // Force camera
            var camera = EntityManager.GetComponentData<ServerCameraState>(playerEntity);
            camera.Target = chrEntity;
            EntityManager.SetComponentData(playerEntity, camera);
        }
    }
}