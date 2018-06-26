using System;
using System.Diagnostics;
using System.Reflection;
using EudiFramework;
using package.guerro.shared;
using Scripts.Physics;
using Scripts.Utilities;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.ResourceManagement;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace package.stormium.def
{
    [RequireComponent(typeof(ReferencableGameObject), typeof(GameObjectEntity))]
    [RequireComponent(typeof(CharacterController), typeof(CharacterControllerMotor))]
    public class CreateCompleteCharacter : MonoBehaviour
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
            BaseVerticalForce    = 0.25f,
            VerticalForceDelta   = 0.05f,
            MaximalVerticalForce = 0.52f,
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

        public static bool hasLoadedScene = false;
        public static RenderPipelineAsset currPipeline;
        private void Awake()
        {
            /*Camera.main.enabled = false;
            Camera.main.enabled = true;*/

            currPipeline = GraphicsSettings.renderPipelineAsset;
            GraphicsSettings.renderPipelineAsset = null;
            

            var goe = gameObject.GetComponent<GameObjectEntity>();

            /*goe.EntityManager.AddComponentData(goe.Entity, new StCharacter());
            goe.EntityManager.AddComponentData(goe.Entity, new DefStCharacterHead());
            goe.EntityManager.AddComponentData(goe.Entity, new DefStVelocity());
            goe.EntityManager.AddComponentData(goe.Entity, MvRunData);
            goe.EntityManager.AddComponentData(goe.Entity, new DefStMvRunInput());
            goe.EntityManager.AddComponentData(goe.Entity, new CameraTargetData());*/
            goe.EntityManager.AddComponentData(goe.Entity, new DefStMvInformation());

            var referencableGameObject = ReferencableGameObject.GetComponent<ReferencableGameObject>(gameObject);

            gameObject.AddComponent<StCharacterWrapper>();
            gameObject.AddComponent<DefStMvStaminaWrapper>();
            gameObject.AddComponent<DefStCharacterHeadWrapper>();
            gameObject.AddComponent<DefStVelocityWrapper>();
            gameObject.AddComponent<DefStMvGravityWrapper>();
            gameObject.AddComponent<DefStMvJumpWrapper>();
            gameObject.AddComponent<DefStMvDodgeWrapper>();
            gameObject.AddComponent<DefStMvDodgeOnGroundWrapper>();
            gameObject.AddComponent<DefStMvDodgeOnWallWrapper>();
            gameObject.AddComponent<DefStMvWalljumpWrapper>();
            gameObject.AddComponent<DefStMvRunWrapper>();
            gameObject.AddComponent<DefStMvGroundEnvironnementWrapper>();
            gameObject.AddComponent<DefStMvAirEnvironnementWrapper>();
            gameObject.AddComponent<DefStMvInputWrapper>();
            gameObject.AddComponent<CameraTargetComponent>();

            goe.EntityManager.SetComponentData(goe.Entity, MvRunData);
            goe.EntityManager.SetComponentData(goe.Entity, MvJumpData);
            goe.EntityManager.SetComponentData(goe.Entity, MvDodgeOnGroundData);
            goe.EntityManager.SetComponentData(goe.Entity, GroundSettings);
            goe.EntityManager.SetComponentData(goe.Entity, AirSettings);
            goe.EntityManager.SetComponentData(goe.Entity, MvDodgeData);

            int trueCount = 0;
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
            Debug.Log(trueCount + ", f1 ms: " + watch.Elapsed.ToString());

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
        }
        
        private Vector3 m_LastPosition;
        private float m_Speed;

        public Animator Animator;

        private void LateUpdate()
        {
            if (!hasLoadedScene && Time.frameCount == 5)
            {
                hasLoadedScene = true;
                GraphicsSettings.renderPipelineAsset = currPipeline;
            }
            
            var flatPosition = transform.position.ToGrid(1);
            
            m_Speed        = (flatPosition - m_LastPosition).magnitude / Time.deltaTime;
            m_LastPosition = flatPosition;

            var isGrounded = GetComponent<CharacterControllerMotor>().IsGrounded();
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
            Animator.SetBool("InAir", !isGrounded);
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