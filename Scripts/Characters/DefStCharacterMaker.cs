using package.stormium.core;
using package.stormium.def.actions;
using package.stormium.def.Movements.Data;
using package.stormiumteam.shared;
using package.stormiumteam.shared.online;
using Scripts.Movements.Data;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace package.stormium.def.characters
{
    public static class DefStCharacterMaker
    {
        public static Entity New(StCharacterBaseMaker characterBaseMaker)
        {
            var world = World.Active;
            var em = world.GetExistingManager<EntityManager>();
            var entity = characterBaseMaker.CreateEntity();
            entity = entity == Entity.Null ? em.CreateEntity() : entity;
            
            characterBaseMaker.Create(entity);

            return entity;
        }

        public static void AddAction(Entity character, StActionBaseMaker actionBaseMaker)
        {
            
        }
    }

    public abstract class StCharacterBaseMaker
    {
        public EntityManager EntityManager => World.Active.GetExistingManager<EntityManager>();

        public abstract Entity CreateEntity();
        public abstract void Create(Entity entity);

        public void AddDefaultComponents(Entity entity)
        {
            // Add tags
            EntityManager.AddComponentData(entity, new CharacterTag());
            EntityManager.AddComponentData(entity, new StCharacter());

            // Add misc
            EntityManager.AddComponentData(entity, new StHealth(100));
            EntityManager.AddComponentData(entity, new StMaxHealth(100));

            // Add actions
            EntityManager.AddBuffer<StActionContainer>(entity);
        }
    }

    public abstract class StActionBaseMaker
    {
        public virtual void Create()
        {
            
        }
    }

    public class StormiumCharacterMaker : StCharacterBaseMaker
    {
        public GameObject GameObject;
        public GameObjectEntity GameObjectEntity;
        public GamePlayer Player;
        
        public StormiumCharacterMaker(GamePlayer player)
        {
            Player = player;

            if (!player.IsCreated)
            {
                Debug.LogWarning("!player.IsCreated");
            }
        }
        
        public override Entity CreateEntity()
        {
            GameObject = new GameObject("Character",
                typeof(CharacterController),
                typeof(CharacterControllerMotor),
                typeof(ReferencableGameObject),
                typeof(GameObjectEntity));

            GameObject.GetComponent<ReferencableGameObject>().Refresh();
            
            return (GameObjectEntity = GameObject.GetComponent<GameObjectEntity>()).Entity;
        }
        
        public override void Create(Entity entity)
        {
            Debug.Assert(GameObject != null, "GameObject != null");
            Debug.Assert(GameObjectEntity.Entity == entity, "GameObjectEntity.Entity == entity");
            
            
            var characterController = GameObject.GetComponent<CharacterController>();
            characterController.center     = Vector3.up;
            characterController.radius     = 0.3f;
            characterController.stepOffset = 0.4f;
            characterController.height     = 2f;
            characterController.detectCollisions = false;
            
            AddDefaultComponents(entity);
            
            if (Player.IsCreated) EntityManager.AddComponentData(entity, new CharacterPlayerOwner
            {
                Target = Player.WorldPointer
            });

            var rocketEntity = Entity.Null;
            // Add rocket
            {
                var actionEntity = EntityManager.CreateEntity
                (
                    typeof(StActionTag),
                    typeof(StActionOwner),
                    typeof(StActionAmmo),
                    typeof(StActionAmmoCooldown),
                    typeof(StActionSlot),
                    typeof(StDefActionRocketLauncher)
                );

                var actionType = typeof(StDefActionRocketLauncher);

                EntityManager.SetComponentData(actionEntity, new StActionTag(TypeManager.GetTypeIndex(actionType)));
                EntityManager.SetComponentData(actionEntity, new StActionOwner(entity));
                EntityManager.SetComponentData(actionEntity, new StActionSlot(-1));
                EntityManager.SetComponentData(actionEntity, new StActionAmmo(0, 2, 0));
                EntityManager.SetComponentData(actionEntity, new StActionAmmoCooldown(-1, 1.5f));
                EntityManager.SetComponentData(actionEntity, new StDefActionRocketLauncher(42, 25));

                EntityManager.GetBuffer<StActionContainer>(entity).Add(new StActionContainer(actionEntity));
                rocketEntity = actionEntity;
            }
            // Add switcher
            {
                var actionEntity = EntityManager.CreateEntity
                (
                    typeof(StActionTag),
                    typeof(StActionOwner),
                    typeof(StActionSlot),
                    typeof(StActionDualSwitch)
                );

                var actionType = typeof(StActionDualSwitch);

                EntityManager.SetComponentData(actionEntity, new StActionTag(TypeManager.GetTypeIndex(actionType)));
                EntityManager.SetComponentData(actionEntity, new StActionOwner(entity));
                EntityManager.SetComponentData(actionEntity, new StActionSlot(0));
                EntityManager.SetComponentData(actionEntity, new StActionDualSwitch(rocketEntity, rocketEntity));

                EntityManager.GetBuffer<StActionContainer>(entity).Add(new StActionContainer(actionEntity));
            }
            // Add grenade
            {
                var actionEntity = EntityManager.CreateEntity
                (
                    typeof(StActionTag),
                    typeof(StActionOwner),
                    typeof(StActionSlot),
                    typeof(StActionAmmo),
                    typeof(StActionAmmoCooldown),
                    typeof(StDefActionThrowGrenade)
                );

                var actionType = typeof(StDefActionThrowGrenade);

                EntityManager.SetComponentData(actionEntity, new StActionTag(TypeManager.GetTypeIndex(actionType)));
                EntityManager.SetComponentData(actionEntity, new StActionOwner(entity));
                EntityManager.SetComponentData(actionEntity, new StActionSlot(1));
                EntityManager.SetComponentData(actionEntity, new StActionAmmo(0, 1, 0));
                EntityManager.SetComponentData(actionEntity, new StActionAmmoCooldown(-1, 8));
                EntityManager.SetComponentData(actionEntity, new StDefActionThrowGrenade(28f, 25, 2));

                EntityManager.GetBuffer<StActionContainer>(entity).Add(new StActionContainer(actionEntity));
            }

            // Add transforms component
            EntityManager.AddComponentData(entity, new Position());
            EntityManager.AddComponentData(entity, new Rotation());
            EntityManager.AddComponentData(entity, new StVelocity());

            // Add settings
            EntityManager.AddComponentData(entity, new EntityPhysicLayer(30));
            EntityManager.AddComponentData(entity, DefStGroundRunSettings.NewBase());
            EntityManager.AddComponentData(entity, DefStAerialRunSettings.NewBase());
            EntityManager.AddComponentData(entity, DefStJumpSettings.NewBase());
            EntityManager.AddComponentData(entity, DefStDodgeOnGroundSettings.NewBase());

            // Add simple client inputs
            EntityManager.AddComponentData(entity, new DefStRunClientInput());
            EntityManager.AddComponentData(entity, new DefStJumpClientInput());
            EntityManager.AddComponentData(entity, new DefStDodgeClientInput());
            EntityManager.AddComponentData(entity, new DefStEntityAimClientInput());

            // Add global inputs
            EntityManager.AddComponentData(entity, new DefStRunInput());
            EntityManager.AddComponentData(entity, new DefStJumpInput());
            EntityManager.AddComponentData(entity, new DefStDodgeInput());
            EntityManager.AddComponentData(entity, new DefStEntityAimInput());

            // Add processable data
            EntityManager.AddComponentData(entity, new DefStJumpProcessData());
            EntityManager.AddComponentData(entity, new DefStDodgeOnGroundProcessData());

            // Make the clients able to change the input data
            EntityManager.AddComponentData(entity, new ClientDriveData<DefStRunInput>(Player));
            EntityManager.AddComponentData(entity, new ClientDriveData<DefStJumpInput>(Player));
            EntityManager.AddComponentData(entity, new ClientDriveData<DefStDodgeInput>(Player));
            EntityManager.AddComponentData(entity, new ClientDriveData<DefStEntityAimInput>(Player));
        }
    }
}