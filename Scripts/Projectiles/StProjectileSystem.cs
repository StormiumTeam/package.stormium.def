using package.stormium.core;
using package.stormium.def;
using package.stormiumteam.networking;
using package.stormiumteam.shared;
using Scripts;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Jobs;

namespace package.stormium.def.actions
{
    public abstract class StProjectileSystem : GameComponentSystem
    {
        protected struct ControllerGroup
        {
            public ComponentDataArray<Position>        PositionArray;
            public ComponentDataArray<Rotation>        RotationArray;
            public ComponentDataArray<StVelocity>      VelocityArray;
            public ComponentArray<CharacterController> ControllerArray;
            public EntityArray                         EntityArray;

            public readonly int Length;
        }

        protected struct RigidbodyGroup
        {
            public ComponentArray<Rigidbody> RigidbodyArray;
            public GameObjectArray           GameObjectArray;
            public TransformAccessArray      TransformArray;
            public EntityArray               EntityArray;

            public readonly int Length;
        }

        [Inject] protected ControllerGroup AllControllers;
        [Inject] protected RigidbodyGroup AllRigidbodies;
        
        public (Entity send, Entity receive) AskPhysic_Start(Entity caller, Entity sendContract = default)
        {
            if (sendContract == Entity.Null)
            {
                sendContract = EntityManager.CreateEntity(typeof(EntityContractTag), typeof(EntitySendContract));
            }
            
            var receiveContract = EntityManager.CreateEntity(typeof(EntityContractTag), typeof(EntityReceiveContract));
            foreach (var obj in AppEvent<StEventAskPhysicObjects.IEv>.eventList)
            {
                AppEvent<StEventAskPhysicObjects.IEv>.Caller = this;
                obj.CallbackStartOnAskPhysicObjects(new StEventAskPhysicObjects.StartArguments(caller, sendContract, receiveContract));
            }

            return (sendContract, receiveContract);
        }

        public void AskPhysic_End(Entity caller, Entity sendContract, Entity receiveContract)
        {
            foreach (var obj in AppEvent<StEventAskPhysicObjects.IEv>.eventList)
            {
                AppEvent<StEventAskPhysicObjects.IEv>.Caller = this;
                obj.CallbackEndOnAskPhysicObjects(new StEventAskPhysicObjects.EndArguments(caller, sendContract, receiveContract));
            }
            
            PostUpdateCommands.DestroyEntity(sendContract);
            PostUpdateCommands.DestroyEntity(receiveContract);
        }
        
        public (Entity send, Entity receive) AskEntityInProjectile_Start(Entity caller, Entity victim, Entity sendContract = default)
        {
            if (sendContract == Entity.Null)
            {
                sendContract = EntityManager.CreateEntity(typeof(EntityContractTag), typeof(EntitySendContract));
            }
            
            var receiveContract = EntityManager.CreateEntity(typeof(EntityContractTag), typeof(EntityReceiveContract));
            foreach (var obj in AppEvent<StEventOnProjectileHit.IEv>.eventList)
            {
                AppEvent<StEventOnProjectileHit.IEv>.Caller = this;
                obj.CallbackStartOnProjectileHitBegin(new StEventOnProjectileHit.StartArguments(caller, victim, sendContract, receiveContract));
            }

            return (sendContract, receiveContract);
        }

        public void AskEntityInProjectile_End(Entity caller, Entity victim, Entity sendContract, Entity receiveContract)
        {
            foreach (var obj in AppEvent<StEventOnProjectileHit.IEv>.eventList)
            {
                AppEvent<StEventOnProjectileHit.IEv>.Caller = this;
                obj.CallbackEndOnOnProjectileHitEnd(new StEventOnProjectileHit.EndArguments(caller, victim, sendContract, receiveContract));
            }
            
            PostUpdateCommands.DestroyEntity(sendContract);
            PostUpdateCommands.DestroyEntity(receiveContract);
        }

        public Entity EndProjectile()
        {
            return EntityManager.CreateEntity(typeof(EntityContractTag), typeof(EntitySendContract), typeof(SendContractProjectileExplode));
        }

        public Entity DiffuseEndProjectile(Entity caller, Entity sendContract)
        {
            var receiveContract = EntityManager.CreateEntity(typeof(EntityContractTag), typeof(EntityReceiveContract));
            foreach (var obj in AppEvent<StEventProjectileEnd.IEv>.eventList)
            {
                AppEvent<StEventProjectileEnd.IEv>.Caller = this;
                obj.CallbackOnProjectileEnd(new StEventProjectileEnd.Arguments(caller, sendContract, receiveContract));
            }
            
            PostUpdateCommands.DestroyEntity(sendContract);
            PostUpdateCommands.DestroyEntity(receiveContract);

            return receiveContract;
        }
    }

    public abstract class StEventProjectileEnd
    {
        public struct Arguments : IDelayComponentArguments
        {
            public            Entity Projectile;
            [ReadOnly] public Entity SendContract;
            public            Entity ReceiveContract;

            public Arguments(Entity projectile, [ReadOnly] Entity sendContract, Entity receiveContract)
            {
                Projectile      = projectile;
                SendContract    = sendContract;
                ReceiveContract = receiveContract;
            }
        }

        public interface IEv : IAppEvent
        {
            void CallbackOnProjectileEnd(Arguments args);
        }

        internal abstract void Sealed();
    }
}