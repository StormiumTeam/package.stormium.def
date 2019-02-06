using package.stormiumteam.shared;
using Runtime.Components;
using Runtime.Data;
using Stormium.Core;
using Stormium.Default.States;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine.Profiling;

namespace Stormium.Default
{
    [UpdateAfter(typeof(PreLateUpdate))]
    public class UpdateCameraSystem : ComponentSystem
    {
        struct DataToSet
        {
            public CameraMode LastSuperiorMode;

            public Entity     Target;
            public float3     PosOffset;
            public quaternion RotOffset;
        }

        struct CameraData
        {
            public Camera camera;
            public Entity entity;
        }

        protected override void OnCreateManager()
        {
            // Create a default local camera state
            var camStateEntity = EntityManager.CreateEntity(typeof(LocalCameraState));

            // Create a default free move camera
            var freeMoveEntity = EntityManager.CreateEntity(typeof(LocalCameraFreeMove), typeof(CameraModifierData), typeof(Position), typeof(Rotation));

            EntityManager.SetComponentData(camStateEntity, new LocalCameraState {Target = freeMoveEntity});
            EntityManager.SetComponentData(freeMoveEntity, new LocalCameraFreeMove{Intensity = 8f});
        }

        private DataToSet dataToSet;
        private CameraData cameraData;
        
        protected override void OnUpdate()
        {
            dataToSet = default;
            cameraData = default;

            if (Input.GetKeyDown(KeyCode.Return))
                Cursor.lockState = CursorLockMode.Locked;
            if (Input.GetKeyDown(KeyCode.Escape))
                Cursor.lockState = CursorLockMode.None;

            Cursor.visible = Cursor.lockState != CursorLockMode.Locked;
            
            Profiler.BeginSample("ForEach1");
            ForEach((Entity entity, GameCamera gameCamera) =>
            {
                if (cameraData.entity != default)
                {
                    Debug.LogWarning($"There is already a game camera, but we found another one? (c={cameraData.entity}, n={entity})");
                    return;
                }

                cameraData.camera = gameCamera.Camera;
                cameraData.entity = entity;
            });
            Profiler.EndSample();

            Profiler.BeginSample("ForEach2");
            ForEach((ref LocalCameraState cameraState) =>
            {
                if (dataToSet.LastSuperiorMode > cameraState.Mode)
                    return;

                dataToSet.LastSuperiorMode = cameraState.Mode;
                dataToSet.Target           = cameraState.Target;
                dataToSet.PosOffset        = default;
                dataToSet.RotOffset        = default;
            });
            Profiler.EndSample();

            Profiler.BeginSample("ForEach3");
            ForEach((ref StGamePlayer player, ref ServerCameraState cameraState) =>
            {
                if (player.IsSelf == 0)
                    return;
                
                if (dataToSet.LastSuperiorMode > cameraState.Mode)
                    return;

                dataToSet.LastSuperiorMode = cameraState.Mode;
                dataToSet.Target           = cameraState.Target;
                dataToSet.PosOffset        = cameraState.PosOffset;
                dataToSet.RotOffset        = cameraState.RotOffset;
            });
            Profiler.EndSample();

            if (cameraData.entity == default)
            {
                Debug.LogError("No Game Camera found?");
                return;
            }

            Compute(cameraData.camera);
        }

        private void Compute(Camera camera)
        {
            if (dataToSet.Target == default)
            {
                Debug.LogWarning("No target found");
                return;
            }
            
            if (!math.all(dataToSet.RotOffset.value))
                dataToSet.RotOffset = quaternion.identity;

            var modifier = EntityManager.GetComponentData<CameraModifierData>(dataToSet.Target);
            var tr       = camera.transform;

            //camera.fieldOfView = modifier.FieldOfView;

            tr.position = modifier.Position + dataToSet.PosOffset;
            tr.rotation = math.mul(modifier.Rotation, dataToSet.RotOffset);
        }
    }
}