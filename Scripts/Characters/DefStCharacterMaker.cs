using LiteNetLib;
using package.stormium.core;
using package.stormium.def.actions;
using package.stormium.def.Movements;
using package.stormium.def.Movements.Data;
using package.stormiumteam.networking.ecs;
using package.stormiumteam.shared;
using package.stormiumteam.shared.online;
using Scripts.Movements.Data;
using Scripts.Movements.MvJump;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace package.stormium.def.characters
{
    public static class DefStEntityMaker
    {
        public static Entity New(StBaseMaker baseMaker)
        {
            var world = World.Active;
            var em = world.GetExistingManager<EntityManager>();
            var entity = baseMaker.CreateEntityInternal();
            entity = entity == Entity.Null ? em.CreateEntity() : entity;
            
            baseMaker.Create(entity);

            return entity;
        }
    }

    public abstract class StBaseMaker
    {
        public EntityManager EntityManager => World.Active.GetExistingManager<EntityManager>();
        public Entity Result { get; private set; }

        public Entity CreateEntityInternal()
        {
            return Result = CreateEntity();
        }

        public abstract Entity CreateEntity();
        public abstract void   Create(Entity entity);
    }
    
    public abstract class StCharacterBaseMaker : StBaseMaker
    {
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

            ShareManager.SetShareOption(entity, typeof(CharacterTag), ComponentShareOption.Automatic, DeliveryMethod.ReliableOrdered);
            ShareManager.SetShareOption(entity, typeof(StCharacter), ComponentShareOption.Automatic, DeliveryMethod.ReliableOrdered);
            ShareManager.SetShareOption(entity, typeof(StHealth), ComponentShareOption.Automatic, DeliveryMethod.ReliableOrdered);
            ShareManager.SetShareOption(entity, typeof(StMaxHealth), ComponentShareOption.Automatic, DeliveryMethod.ReliableOrdered);
        }

        public void AddWeapon(Entity weapEntity, int slot)
        {
            if (EntityManager.HasComponent(weapEntity, typeof(StActionSlot)))
                EntityManager.SetComponentData(weapEntity, new StActionSlot(slot));

            var buffer = EntityManager.GetBuffer<StActionContainer>(Result);
            buffer.Add(new StActionContainer(weapEntity));
        }
    }

    public abstract class StActionBaseMaker : StBaseMaker
    {
    }
    
    public class StormiumRocketAction : StActionBaseMaker
    {
        public Entity Owner;
        
        public override Entity CreateEntity()
        {
            return EntityManager.CreateEntity
            (
                typeof(StActionTag),
                typeof(StActionOwner),
                typeof(StActionAmmo),
                typeof(StActionAmmoCooldown),
                typeof(StActionSlot),
                typeof(StDefActionRocketLauncher)
            );
        }

        public override void Create(Entity entity)
        {
            var actionType = typeof(StDefActionRocketLauncher);

            EntityManager.SetComponentData(entity, new StActionTag(TypeManager.GetTypeIndex(actionType)));
            EntityManager.SetComponentData(entity, new StActionOwner(Owner));
            EntityManager.SetComponentData(entity, new StActionSlot(-1));
            EntityManager.SetComponentData(entity, new StActionAmmo(0, 2, 0));
            EntityManager.SetComponentData(entity, new StActionAmmoCooldown(-1, 1.5f));
            EntityManager.SetComponentData(entity, new StDefActionRocketLauncher(42, 25));
        }

        public StormiumRocketAction(Entity owner)
        {
            Owner = owner;
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
            characterController.slopeLimit     = 75;
            characterController.detectCollisions = false;
            
            AddDefaultComponents(entity);
            
            if (Player.IsCreated) EntityManager.AddComponentData(entity, new CharacterPlayerOwner
            {
                Target = Player.WorldPointer
            });
            
            EntityManager.AddComponentData(entity, new CharacterControllerState(false, false, false, Vector3.up));
            EntityManager.AddComponentData(entity, StormiumCharacterMvData.NewBase());
            EntityManager.AddComponentData(entity, new StormiumCharacterMvProcessData());
            
            ShareManager.SetShareOption(entity, typeof(CharacterPlayerOwner), ComponentShareOption.Automatic, DeliveryMethod.ReliableOrdered);
            ShareManager.SetShareOption(entity, typeof(CharacterControllerState), ComponentShareOption.Automatic, DeliveryMethod.ReliableOrdered);
            ShareManager.SetShareOption(entity, typeof(StormiumCharacterMvData), ComponentShareOption.Automatic, DeliveryMethod.ReliableOrdered);
            ShareManager.SetShareOption(entity, typeof(StormiumCharacterMvProcessData), ComponentShareOption.Automatic, DeliveryMethod.ReliableOrdered);

            /*// Add switcher
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
            }*/

            // Add transforms component
            EntityManager.AddComponentData(entity, new Position());
            EntityManager.AddComponentData(entity, new Rotation());
            EntityManager.AddComponentData(entity, new StVelocity());
            
            ShareManager.SetShareOption(entity, typeof(Position), ComponentShareOption.Manual, DeliveryMethod.ReliableOrdered);
            ShareManager.SetShareOption(entity, typeof(Rotation), ComponentShareOption.Manual, DeliveryMethod.ReliableOrdered);
            ShareManager.SetShareOption(entity, typeof(StVelocity), ComponentShareOption.Manual, DeliveryMethod.ReliableOrdered);
            
            // Add settings
            EntityManager.AddComponentData(entity, new EntityPhysicLayer(30));
            EntityManager.AddComponentData(entity, new StStamina(4, 0.75f, 0));
            EntityManager.AddComponentData(entity, DefStGroundRunSettings.NewBase());
            EntityManager.AddComponentData(entity, DefStAerialRunSettings.NewBase());
            EntityManager.AddComponentData(entity, DefStJumpSettings.NewBase());
            EntityManager.AddComponentData(entity, DefStDodgeOnGroundSettings.NewBase());
            EntityManager.AddComponentData(entity, new DefStJumpStaminaUsageData()
            {
                Usage = EStaminaUsage.RemoveStamina,
                RemoveBySpeedFactor01 = 0.01f,
                BaseRemove = 0.1f
            });
            EntityManager.AddComponentData(entity, new DefStDodgeStaminaUsageData()
            {
                Usage      = EStaminaUsage.BlockAction,
                Needed     = 1f,
                BaseRemove = 1f
            });
            
            ShareManager.SetShareOption(entity, typeof(EntityPhysicLayer), ComponentShareOption.Automatic, DeliveryMethod.ReliableOrdered);
            ShareManager.SetShareOption(entity, typeof(StStamina), ComponentShareOption.Automatic, DeliveryMethod.ReliableOrdered);
            ShareManager.SetShareOption(entity, typeof(DefStGroundRunSettings), ComponentShareOption.Automatic, DeliveryMethod.ReliableOrdered);
            ShareManager.SetShareOption(entity, typeof(DefStAerialRunSettings), ComponentShareOption.Automatic, DeliveryMethod.ReliableOrdered);
            ShareManager.SetShareOption(entity, typeof(DefStJumpSettings), ComponentShareOption.Automatic, DeliveryMethod.ReliableOrdered);
            ShareManager.SetShareOption(entity, typeof(DefStDodgeOnGroundSettings), ComponentShareOption.Automatic, DeliveryMethod.ReliableOrdered);
            ShareManager.SetShareOption(entity, typeof(DefStJumpStaminaUsageData), ComponentShareOption.Automatic, DeliveryMethod.ReliableOrdered);
            ShareManager.SetShareOption(entity, typeof(DefStDodgeStaminaUsageData), ComponentShareOption.Automatic, DeliveryMethod.ReliableOrdered);

            // Add simple client inputs
            EntityManager.AddComponentData(entity, new DefStRunClientInput());
            EntityManager.AddComponentData(entity, new DefStJumpClientInput());
            EntityManager.AddComponentData(entity, new DefStDodgeClientInput());
            EntityManager.AddComponentData(entity, new DefStEntityAimClientInput());
            
            ShareManager.SetShareOption(entity, typeof(DefStRunClientInput), ComponentShareOption.Manual, DeliveryMethod.ReliableOrdered);
            ShareManager.SetShareOption(entity, typeof(DefStJumpClientInput), ComponentShareOption.Manual, DeliveryMethod.ReliableOrdered);
            ShareManager.SetShareOption(entity, typeof(DefStDodgeClientInput), ComponentShareOption.Manual, DeliveryMethod.ReliableOrdered);
            ShareManager.SetShareOption(entity, typeof(DefStEntityAimClientInput), ComponentShareOption.Manual, DeliveryMethod.ReliableOrdered);

            // Add global inputs
            EntityManager.AddComponentData(entity, new DefStRunInput());
            EntityManager.AddComponentData(entity, new DefStJumpInput());
            EntityManager.AddComponentData(entity, new DefStDodgeInput());
            EntityManager.AddComponentData(entity, new DefStEntityAimInput());
            
            ShareManager.SetShareOption(entity, typeof(DefStRunInput), ComponentShareOption.Manual, DeliveryMethod.ReliableOrdered);
            ShareManager.SetShareOption(entity, typeof(DefStJumpInput), ComponentShareOption.Manual, DeliveryMethod.ReliableOrdered);
            ShareManager.SetShareOption(entity, typeof(DefStDodgeInput), ComponentShareOption.Manual, DeliveryMethod.ReliableOrdered);
            ShareManager.SetShareOption(entity, typeof(DefStEntityAimInput), ComponentShareOption.Manual, DeliveryMethod.ReliableOrdered);

            // Add processable data
            EntityManager.AddComponentData(entity, new DefStJumpProcessData());
            EntityManager.AddComponentData(entity, new DefStDodgeOnGroundProcessData());
            EntityManager.AddComponentData(entity, new DefStWallJumpProcessData());
            EntityManager.AddComponentData(entity, new DefStWallDodgeProcessData());
            
            ShareManager.SetShareOption(entity, typeof(DefStJumpProcessData), ComponentShareOption.Manual, DeliveryMethod.ReliableOrdered);
            ShareManager.SetShareOption(entity, typeof(DefStDodgeOnGroundProcessData), ComponentShareOption.Manual, DeliveryMethod.ReliableOrdered);
            ShareManager.SetShareOption(entity, typeof(DefStWallDodgeProcessData), ComponentShareOption.Manual, DeliveryMethod.ReliableOrdered);

            // Make the clients able to change the input data
            EntityManager.AddComponentData(entity, new ClientDriveData<DefStRunInput>(Player));
            EntityManager.AddComponentData(entity, new ClientDriveData<DefStJumpInput>(Player));
            EntityManager.AddComponentData(entity, new ClientDriveData<DefStDodgeInput>(Player));
            EntityManager.AddComponentData(entity, new ClientDriveData<DefStEntityAimInput>(Player));
            
            ShareManager.SetShareOption(entity, typeof(ClientDriveData<DefStRunInput>), ComponentShareOption.Automatic, DeliveryMethod.ReliableOrdered);
            ShareManager.SetShareOption(entity, typeof(ClientDriveData<DefStJumpInput>), ComponentShareOption.Automatic, DeliveryMethod.ReliableOrdered);
            ShareManager.SetShareOption(entity, typeof(ClientDriveData<DefStDodgeInput>), ComponentShareOption.Automatic, DeliveryMethod.ReliableOrdered);
            ShareManager.SetShareOption(entity, typeof(ClientDriveData<DefStEntityAimInput>), ComponentShareOption.Automatic, DeliveryMethod.ReliableOrdered);
        }
    }
}