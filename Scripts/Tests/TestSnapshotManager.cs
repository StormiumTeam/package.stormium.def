using System.Threading;
using package.stormiumteam.networking.runtime.lowlevel;
using package.stormiumteam.shared;
using Stormium.Core.Networking;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using Random = UnityEngine.Random;

namespace Stormium.Default.Tests
{
    public class TestSnapshotManager : ComponentSystem, INativeEventOnGUI
    {
        private ComponentGroup m_ComponentGroup;

        protected override void OnCreateManager()
        {
            m_ComponentGroup = GetComponentGroup(typeof(GenerateEntitySnapshot), typeof(SnapshotEntityDataTransformSystem.State), typeof(SnapshotEntityDataVelocitySystem.State));

            for (var i = 0; i != 1024; i++)
            {
                var e = EntityManager.CreateEntity
                (
                    typeof(SnapshotEntityDataTransformSystem.State),
                    typeof(DataChanged<SnapshotEntityDataTransformSystem.State>),
                    typeof(SnapshotEntityDataVelocitySystem.State),
                    typeof(DataChanged<SnapshotEntityDataVelocitySystem.State>),
                    typeof(GenerateEntitySnapshot)
                );
                EntityManager.SetComponentData(e, new SnapshotEntityDataTransformSystem.State
                {
                    Position = new float3(i * 2, i * 4, i * 8)
                });
            }
            
           World.GetOrCreateManager<AppEventSystem>()
                .SubscribeTo<INativeEventOnGUI, INativeEventOnGUI>(this);
        }

        protected override void OnUpdate()
        {
            var length = m_ComponentGroup.CalculateLength();
            var states = m_ComponentGroup.GetComponentDataArray<SnapshotEntityDataTransformSystem.State>();
            var entityArray = m_ComponentGroup.GetEntityArray();
            
            for (var i = 0; i != length; i++)
            {
                var entity = entityArray[i];

                states[i] = new SnapshotEntityDataTransformSystem.State
                {
                    Position = new float3(Random.Range(-1, 1))
                };
            }
        }

        public void NativeOnGUI()
        {
            GUILayout.BeginVertical();
            GUILayout.Label($"1/DT: {1 / Time.deltaTime}");
            GUILayout.EndVertical();
        }
    }
}