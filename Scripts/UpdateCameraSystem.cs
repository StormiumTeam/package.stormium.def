using Stormium.Core;
using Stormium.Default.States;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Data;
using StormiumTeam.Shared.Gen;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Profiling;

namespace Stormium.Default
{
    [UpdateInGroup(typeof(STUpdateOrder.UO_FinalizeData))]
    public class UpdateCameraSystem : ComponentSystem
    {
        protected override void OnCreate()
        {
            // Create a default local camera state
            var camStateEntity = EntityManager.CreateEntity(typeof(LocalCameraState));

            // Create a default free move camera
            var freeMoveEntity = EntityManager.CreateEntity(typeof(LocalCameraFreeMove), typeof(CameraModifierData), typeof(Translation), typeof(Rotation), typeof(LocalToWorld));

            EntityManager.SetComponentData(camStateEntity, new LocalCameraState
            {
                Data = new CameraState
                {
                    Target = freeMoveEntity
                }
            });
            EntityManager.SetComponentData(freeMoveEntity, new LocalCameraFreeMove {Intensity = 8f});

            m_CameraQuery            = GetEntityQuery(typeof(GameCamera));
            m_LocalCameraStateQuery  = GetEntityQuery(typeof(LocalCameraState));
            m_ServerCameraStateQuery = GetEntityQuery(typeof(ServerCameraState), typeof(GamePlayerLocalTag), typeof(GamePlayer));
        }

        private EntityQuery m_CameraQuery;
        private EntityQuery m_LocalCameraStateQuery;
        private EntityQuery m_ServerCameraStateQuery;

        protected override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.Return))
                Cursor.lockState = CursorLockMode.Locked;
            if (Input.GetKeyDown(KeyCode.Escape))
                Cursor.lockState = CursorLockMode.None;

            Cursor.visible = Cursor.lockState != CursorLockMode.Locked;

            Camera camera       = default;
            Entity cameraEntity = default;

            GameCamera gameCamera = default;
            foreach (var e in this.ToEnumerator_C(m_CameraQuery, ref gameCamera))
            {
                if (cameraEntity != default)
                {
                    Debug.LogWarning($"There is already a game camera, but we found another one? (c={camera}, n={cameraEntity})");
                    return;
                }

                camera       = gameCamera.Camera;
                cameraEntity = e.Entity;
            }

            var lastSuperiorMode = CameraMode.Default;
            var target           = Entity.Null;
            var offset           = RigidTransform.identity;

            LocalCameraState localCameraState = default;
            foreach (var e in this.ToEnumerator_D(m_LocalCameraStateQuery, ref localCameraState))
            {
                if (lastSuperiorMode > localCameraState.Mode)
                    return;

                lastSuperiorMode = localCameraState.Mode;
                target           = localCameraState.Target;
                offset           = localCameraState.Offset;
            }

            ServerCameraState serverCameraState = default;
            foreach (var e in this.ToEnumerator_D(m_ServerCameraStateQuery, ref serverCameraState))
            {
                if (lastSuperiorMode > serverCameraState.Mode)
                    return;

                lastSuperiorMode = serverCameraState.Mode;
                target           = serverCameraState.Target;
                offset           = serverCameraState.Offset;
            }

            if (cameraEntity == default || camera == null)
            {
                Debug.LogError("No Game Camera found?");
                return;
            }

            // ------- ------ ------ //
            // Compute camera data
            // ------- ------ ------ //
            if (target == default)
            {
                Debug.LogWarning("No target found");
                return;
            }

            if (!math.all(offset.rot.value))
                offset.rot = quaternion.identity;

            var tr             = camera.transform;
            var modifierOffset = RigidTransform.identity;
            if (EntityManager.HasComponent<CameraModifierData>(target))
            {
                var modifier = EntityManager.GetComponentData<CameraModifierData>(target);
                camera.fieldOfView = math.max(modifier.FieldOfView, 30);

                modifierOffset = new RigidTransform(modifier.Rotation, modifier.Position);
            }

            tr.position = modifierOffset.pos + offset.pos;
            tr.rotation = math.mul(modifierOffset.rot, offset.rot);
        }
    }
}