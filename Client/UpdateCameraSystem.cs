using Unity.NetCode;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Components;
using StormiumTeam.GameBase.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace CharacterController
{
    [UpdateInGroup(typeof(ClientPresentationSystemGroup))]
    public class UpdateCameraSystem : ComponentSystem
    {
        protected override void OnCreate()
        {
            m_CameraQuery            = GetEntityQuery(typeof(GameCamera));
            m_LocalCameraStateQuery  = GetEntityQuery(typeof(LocalCameraState));
            m_ServerCameraStateQuery = GetEntityQuery(typeof(ServerCameraState), typeof(GamePlayerLocalTag), typeof(GamePlayer));

            RequireForUpdate(m_CameraQuery);

            m_LastSuperiorMode = new NativeArray<int>(1, Allocator.Persistent);
            m_Target           = new NativeArray<Entity>(1, Allocator.Persistent);
            m_Offset           = new NativeArray<RigidTransform>(1, Allocator.Persistent);
        }

        private NativeArray<int>            m_LastSuperiorMode;
        private NativeArray<Entity>         m_Target;
        private NativeArray<RigidTransform> m_Offset;

        private EntityQuery m_CameraQuery;
        private EntityQuery m_LocalCameraStateQuery;
        private EntityQuery m_ServerCameraStateQuery;

        [BurstCompile]
        private struct GetLocalCameraJob : IJobForEach_C<LocalCameraState>
        {
            public NativeArray<int>            LastSuperiorMode;
            public NativeArray<Entity>         Target;
            public NativeArray<RigidTransform> Offset;

            public void Execute(ref LocalCameraState state)
            {
                if (LastSuperiorMode[0] > (int) state.Mode)
                    return;

                LastSuperiorMode[0] = (int) state.Mode;
                Target[0]           = state.Target;
                Offset[0]           = state.Offset;
            }
        }

        [BurstCompile]
        private struct GetServerCameraJob : IJobForEach_C<ServerCameraState>
        {
            public NativeArray<int>            LastSuperiorMode;
            public NativeArray<Entity>         Target;
            public NativeArray<RigidTransform> Offset;

            public void Execute(ref ServerCameraState state)
            {
                if (LastSuperiorMode[0] > (int) state.Mode)
                    return;

                LastSuperiorMode[0] = (int) state.Mode;
                Target[0]           = state.Target;
                Offset[0]           = state.Offset;
            }
        }

        protected override void OnUpdate()
        {
            if (m_LocalCameraStateQuery.IsEmptyIgnoreFilter && m_ServerCameraStateQuery.IsEmptyIgnoreFilter)
                return;

            if (Input.GetKeyDown(KeyCode.Return))
                Cursor.lockState = CursorLockMode.Locked;
            if (Input.GetKeyDown(KeyCode.Escape))
                Cursor.lockState = CursorLockMode.None;

            Cursor.visible = Cursor.lockState != CursorLockMode.Locked;

            var cameraEntity = m_CameraQuery.GetSingletonEntity();
            var camera       = EntityManager.GetComponentObject<Camera>(cameraEntity);

            m_LastSuperiorMode[0] = -1;
            m_Target[0]           = Entity.Null;
            m_Offset[0]           = RigidTransform.identity;

            var handle = new GetLocalCameraJob
            {
                LastSuperiorMode = m_LastSuperiorMode,
                Target           = m_Target,
                Offset           = m_Offset
            }.ScheduleSingle(m_LocalCameraStateQuery);
            handle = new GetServerCameraJob
            {
                LastSuperiorMode = m_LastSuperiorMode,
                Target           = m_Target,
                Offset           = m_Offset
            }.ScheduleSingle(m_ServerCameraStateQuery, handle);

            handle.Complete();

            // ------- ------ ------ //
            // Compute camera data
            // ------- ------ ------ //
            if (m_Target[0] == default)
            {
                //Debug.LogWarning("No target found");
                return;
            }

            Compute(camera, m_Target[0], m_Offset[0]);
        }

        private void Compute(Camera camera, Entity target, RigidTransform offset)
        {

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
            else
            {
                if (EntityManager.HasComponent<Translation>(target))
                    modifierOffset.pos = EntityManager.GetComponentData<Translation>(target).Value;
                if (EntityManager.HasComponent<Rotation>(target))
                    modifierOffset.rot = EntityManager.GetComponentData<Rotation>(target).Value;
            }

            tr.position = modifierOffset.pos + offset.pos;
            tr.rotation = math.mul(modifierOffset.rot, offset.rot);
        }
    }
}