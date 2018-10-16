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