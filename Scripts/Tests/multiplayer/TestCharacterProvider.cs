using System;
using System.Collections.Generic;
using System.Diagnostics;
using package.stormium.def.Kits.ProKit;
using Runtime;
using Runtime.Data;
using StandardAssets.Characters.Physics;
using Stormium.Core;
using Stormium.Default.States;
using StormiumShared.Core.Networking;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine.ResourceManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Stormium.Default.Tests
{
    [UpdateBefore(typeof(PreUpdate))]
    public class TestCharacterProvider : SystemProvider
    {        
        public struct TestCharacter : IComponentData
        {
        }
        
        private ComponentTypes m_Components;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();
            
            m_Components = new ComponentTypes(new[]
            {
                ComponentType.Create<TestCharacter>(),
                ComponentType.Create<CameraModifierData>(), 
                ComponentType.Create<ModelIdent>(), 
                ComponentType.Create<ProKitBehaviorSettings>(),
                ComponentType.Create<ProKitInputState>(),
                ComponentType.Create<AimLookState>(),
                ComponentType.Create<Velocity>(),
                ComponentType.Create<TransformState>(), 
                ComponentType.Create<TransformStateDirection>(),
                //ComponentType.Create<InterpolationData>(),
                //ComponentType.Create<InterpolationBuffer>(), 
            });
        }

        public override Entity SpawnEntity(Entity origin, StSnapshotRuntime snapshotRuntime)
        {
            var gameObject = new GameObject("ToSet", typeof(Rigidbody), typeof(CapsuleCollider), typeof(OpenCharacterController));
            var goe = gameObject.AddComponent<GameObjectEntity>();

            EntityManager.AddComponents(goe.Entity, m_Components);

            var loadModelBehavior = gameObject.AddComponent<LoadModelFromStringBehaviour>();

            loadModelBehavior.SpawnRoot = gameObject.transform;
            loadModelBehavior.AssetId = "TestCharacter";

            var controller = gameObject.GetComponent<OpenCharacterController>();
            
            controller.SetCenter(new Vector3(0, 1, 0), true, true);

            gameObject.AddComponent<DestroyGameObjectOnEntityDestroyed>();

            var cFlags = snapshotRuntime.Header.Sender.Flags;
            EntityManager.SetComponentData(goe.Entity, cFlags == SnapshotFlags.Local ? new TransformStateDirection(Dir.ConvertToState) : new TransformStateDirection(Dir.ConvertFromState));

            gameObject.name = $"TestCharacter(o={origin}, s={goe.Entity})";
            
            return goe.Entity;
        }

        public override void DestroyEntity(Entity worldEntity)
        {
            var gameObject = EntityManager.GetComponentObject<Transform>(worldEntity).gameObject;
            
            Object.Destroy(gameObject);
        }
    }
}