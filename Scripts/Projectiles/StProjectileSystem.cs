using System;
using System.Linq;
using LiteNetLib.Utils;
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

        public Entity CreateCommand(params ComponentType[] cmdTypes)
        {
            var initArray = new ComponentType[] {typeof(EntityCommand)};
            
            return EntityManager.CreateEntity(initArray.Concat(cmdTypes).ToArray());
        }
        
        public Entity CreateCommandTs(params ComponentType[] cmdTypes)
        {
            var initArray = new ComponentType[] {typeof(EntityCommand), typeof(EntityCommandSource), typeof(EntityCommandTarget)};
            
            return EntityManager.CreateEntity(initArray.Concat(cmdTypes).ToArray());
        }
        
        public Entity CreateCommandResult(params ComponentType[] cmdTypes)
        {
            var initArray = new ComponentType[] {typeof(EntityCommand), typeof(EntityCommandResult)};
            
            return EntityManager.CreateEntity(initArray.Concat(cmdTypes).ToArray());
        }

        public void DiffuseCommand(Entity command, Entity commandResult, bool defaultResult, CmdState state)
        {
            commandResult.SetComponentData(new EntityCommandResult { IsAuthorized = Convert.ToByte(defaultResult) });
            
            foreach (var ev in AppEvent<StEventDiffuseCommand.IEv>.eventList)
            {
                AppEvent<StEventDiffuseCommand.IEv>.Caller = this;
                
                commandResult.SetComponentData(new EntityCommandResult { IsAuthorized = Convert.ToByte(defaultResult) });
                
                ev.OnCommandDiffuse(new StEventDiffuseCommand.Arguments(command, commandResult, state));
            }
        }

        public void StartAskingPhysicObjects(Entity caller, Entity reasonEntity)
        {
            foreach (var ev in AppEvent<StEventAskPhysicObjects.IEv>.eventList)
            {
                AppEvent<StEventAskPhysicObjects.IEv>.Caller = this;
                ev.CallbackStartOnAskPhysicObjects(new StEventAskPhysicObjects.StartArguments(caller, reasonEntity));
            }
        }
        
        public void EndAskingPhysicObjects(Entity caller, Entity reasonEntity)
        {
            foreach (var ev in AppEvent<StEventAskPhysicObjects.IEv>.eventList)
            {
                AppEvent<StEventAskPhysicObjects.IEv>.Caller = this;
                ev.CallbackEndOnAskPhysicObjects(new StEventAskPhysicObjects.EndArguments(caller, reasonEntity));
            }
        }
    }
}