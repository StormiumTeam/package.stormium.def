using System;
using System.Collections.Generic;
using System.Diagnostics;
using package.stormium.def.characters;
using package.stormium.def.Network;
using package.stormiumteam.networking.ecs;
using package.stormiumteam.shared;
using package.stormiumteam.shared.online;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

namespace package.stormium.def
{
    [RequireComponent(typeof(ReferencableGameObject), typeof(GameObjectEntity))]
    [RequireComponent(typeof(CharacterController), typeof(CharacterControllerMotor))]
    public class CreateCompleteCharacter : MonoBehaviour,
        IDefStCharacterOnJump
    {
        public DefStMvRun MvRunData = new DefStMvRun
        {
            Acceleration    = 14f,
            Deacceleration  = 10f,
            AirAcceleration = 2.5f,
        };

        public DefStMvGroundEnvironnement GroundSettings = new DefStMvGroundEnvironnement()
        {
            BaseSpeed        = 8f,
            GroundFriction   = 6f,
            SpeedFrictionMin = 6f,
            SpeedFrictionMax = 32f,
            FrictionMin      = 0.3f,
            FrictionMax      = 1f
        };
        
        public DefStMvAirEnvironnement AirSettings = new DefStMvAirEnvironnement()
        {
            BaseSpeed        = 8f,
            Control = 12.5f,
            AccelerationByHighnessForce = 0f,
        };

        public DefStMvJump MvJumpData = new DefStMvJump()
        {
            BaseVerticalForce      = 0.275f,
            MaximumConsecutiveAirJump = 2,
            MinTimeBetweenJumps    = 0.1f,
            MaxTimeBetweenJumps    = 0.75f,
            GravityComplementForce = 1f
        };

        public DefStMvDodgeOnGround MvDodgeOnGroundData = new DefStMvDodgeOnGround()
        {
            StaminaUse   = 0.25f,
            VerticalBump = 1.25f
        };
        
        public DefStMvDodge MvDodgeData = new DefStMvDodge()
        {
            AdditiveForce = 5f
        };


        private void Awake()
        {            
            var goe = gameObject.GetComponent<GameObjectEntity>();

            /*goe.EntityManager.AddComponentData(goe.Entity, new StCharacter());
            goe.EntityManager.AddComponentData(goe.Entity, new DefStCharacterHead());
            goe.EntityManager.AddComponentData(goe.Entity, new DefStVelocity());
            goe.EntityManager.AddComponentData(goe.Entity, MvRunData);
            goe.EntityManager.AddComponentData(goe.Entity, new DefStMvRunInput());
            goe.EntityManager.AddComponentData(goe.Entity, new CameraTargetData());*/
            goe.EntityManager.AddComponentData(goe.Entity, new DefStMvInformation());

            var referencableGameObject = ReferencableGameObject.GetComponent<ReferencableGameObject>(gameObject);

            referencableGameObject.GetOrAddComponent<StCharacterWrapper>();
            referencableGameObject.GetOrAddComponent<DefStMvStaminaWrapper>();
            referencableGameObject.GetOrAddComponent<StEntityHeadLookAtWrapper>();
            referencableGameObject.GetOrAddComponent<DefStVelocityWrapper>();
            referencableGameObject.GetOrAddComponent<DefStMvGravityWrapper>();
            referencableGameObject.GetOrAddComponent<DefStMvJumpWrapper>();
            referencableGameObject.GetOrAddComponent<DefStMvDodgeWrapper>();
            referencableGameObject.GetOrAddComponent<DefStMvDodgeOnGroundWrapper>();
            referencableGameObject.GetOrAddComponent<DefStMvDodgeOnWallWrapper>();
            referencableGameObject.GetOrAddComponent<DefStMvWalljumpWrapper>();
            referencableGameObject.GetOrAddComponent<DefStMvRunWrapper>();
            referencableGameObject.GetOrAddComponent<DefStMvGroundEnvironnementWrapper>();
            referencableGameObject.GetOrAddComponent<DefStMvAirEnvironnementWrapper>();
            referencableGameObject.GetOrAddComponent<DefStMvInputWrapper>();
            referencableGameObject.GetOrAddComponent<CameraTargetComponent>();

            if (GameObject.Find("use_debug"))
            {
                goe.Entity.SetOrAddComponentData(new NetworkEntity(1, goe.Entity));
                goe.Entity.SetOrAddComponentData(new StCharacter());
                goe.Entity.SetOrAddComponentData(new VoidSystem<DefaultNetSyncTransformDataSystem>());
                goe.Entity.SetOrAddComponentData(new CharacterPlayerOwner()
                {
                    Target = World.Active.GetOrCreateManager<GamePlayerBank>().MainPlayer.WorldPointer
                });
            }
            
            // Add executables
            goe.Entity.SetOrAddComponentData(new DefStMvDodgeOnGroundExecutable());
            goe.Entity.SetOrAddComponentData(new DefStMvJumpExecutable());
            goe.Entity.SetOrAddComponentData(new DefStMvDodgeOnWallExecutable());
            goe.Entity.SetOrAddComponentData(new DefStMvRunExecutable());
            
            goe.Entity.SetOrAddComponentData(new DefStMvJumpState());
            goe.Entity.SetOrAddComponentData(new Position());
            goe.Entity.SetOrAddComponentData(new Rotation());
            //goe.Entity.SetOrAddSharedComponentData(new NetSnapshotPosition(new List<NetSnapshotPosition.Frame>()));
            
            goe.Entity.SetOrAddComponentData(MvRunData);
            goe.Entity.SetOrAddComponentData(MvJumpData);
            goe.Entity.SetOrAddComponentData(MvDodgeOnGroundData);
            goe.Entity.SetOrAddComponentData(GroundSettings);
            goe.Entity.SetOrAddComponentData(AirSettings);
            goe.Entity.SetOrAddComponentData(AirSettings);

            /*int trueCount = 0;
            Type type = null;
            var watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i != 1; i++) // One frame
            {
                for (int s = 0; s != 12; s++) // 12 systems
                {
                    var imovementType = typeof(IMovement);
                    for (int j = 0; j != 32; j++) // 32 players
                    {
                        var allComponents = goe.EntityManager.GetComponentTypes(goe.Entity, Allocator.Temp);
                        var length = allComponents.Length;
                        for (int _ = 0; _ != length; _++)
                        {
                            //var componentType = allComponents[_];
                            //if (imovementType.IsAssignableFrom(typeof(DefStMvRun)))
                                trueCount++;
                        }

                        allComponents.Dispose();
                    }
                }
            }

            watch.Stop();
            Debug.Log(trueCount + ", f1 ms: " + watch.Elapsed.ToString());*/

            var physicGroup = CPhysicSettings.Active.RegisterOrCreateGroup("Characters");
            CPhysicSettings.Active.SetGroup(goe.Entity, physicGroup);

            referencableGameObject.Refresh();

            var entities = goe.EntityManager.GetAllEntities(Allocator.Temp);
            foreach (var entity in entities)
                if (goe.EntityManager.HasComponent<CameraData>(entity))
                {
                    var newCameraData = goe.EntityManager.GetComponentData<CameraData>(entity);
                    newCameraData.TargetId = goe.Entity;
                    goe.EntityManager.SetComponentData(entity, newCameraData);
                }

            entities.Dispose();
            
            World.Active.GetOrCreateManager<AppEventSystem>().SubscribeToAll(this);
        }
        
        private Vector3 m_LastPosition;
        private float m_Speed;

        public Animator[] Animators;

        private void LateUpdate()
        {
            var flatPosition = transform.position.ToGrid(1);
            
            m_Speed        = (flatPosition - m_LastPosition).magnitude / Time.deltaTime;
            m_LastPosition = flatPosition;

            /*var isGrounded = GetComponent<CharacterControllerMotor>().IsGrounded();
            if (!Animator.GetBool("InAir") && !isGrounded)
            {
                Animator.SetTrigger("StartJump");
                Animator.Play("FPP_StartJump");
            }

            if (Animator.GetBool("InAir") && isGrounded)
            {
                Animator.SetTrigger("EndJump");
                Animator.Play("FPP_EndJump");
            }
            Animator.SetBool("InAir", !isGrounded);*/
        }
        
        public void CharacterOnJump(Entity entity)
        {
            foreach (var animator in Animators)
            {
                animator.SetTrigger("CharacterJump");
            }   
        }

        private void OnGUI()
        {            
            var goe = GetComponent<GameObjectEntity>();
            var stamina = Mathf.FloorToInt(goe.EntityManager.GetComponentData<DefStMvStamina>(goe.Entity).Value
                                           * 100);
            var velocity = goe.EntityManager.GetComponentData<DefStVelocity>(goe.Entity).Velocity;

            
            GUI.color = Color.black;
            GUI.Label(new Rect(10.5f, 10.5f, 200, 25), string.Format("Speed: {0:F2}", m_Speed));
            GUI.color = Color.white;
            GUI.Label(new Rect(10, 10, 200, 25), string.Format("Speed: {0:F2}", m_Speed));
            
            GUI.color = Color.black;
            GUI.Label(new Rect(150.5f, 10.5f, 200, 25), $"Vel: {velocity.ToString("F2")}");
            GUI.color = Color.white;
            GUI.Label(new Rect(150, 10, 200, 25), $"Vel: {velocity.ToString("F2")}");

            GUI.color = Color.black;
            GUI.Label(new Rect(10.5f, 30.5f, 200, 25), string.Format("Stamina: {0:F2}", stamina));
            GUI.color = Color.white;
            GUI.Label(new Rect(10, 30, 200, 25), string.Format("Stamina: {0:F2}", stamina));

            var runData = goe.EntityManager.GetComponentData<DefStMvGroundEnvironnement>(goe.Entity);
            var friction = runData.SpeedFrictionMin / Mathf.Clamp(m_Speed, runData.SpeedFrictionMin, runData.SpeedFrictionMax);
            friction = Mathf.Clamp(friction, runData.FrictionMin, runData.FrictionMax);
            
            GUI.color = Color.black;
            GUI.Label(new Rect(10.5f, 60.5f, 200, 25), string.Format("Friction: {0:F2}", friction));
            GUI.color = Color.white;
            GUI.Label(new Rect(10, 60, 200, 25), string.Format("Friction: {0:F2}", friction));
        }
    }
}