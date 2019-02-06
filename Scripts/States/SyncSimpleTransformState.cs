using System;
using package.stormiumteam.networking;
using package.stormiumteam.networking.runtime.lowlevel;
using package.stormiumteam.shared;
using StormiumShared.Core.Networking;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using TransformChanged = StormiumShared.Core.Networking.DataChanged<Stormium.Default.States.TransformState>;

namespace Stormium.Default.States
{
    public class SyncSimpleTransformState : SnapshotEntityDataManualStreamer<TransformState>
    {
        protected override void WriteDataForEntity(int index, Entity entity, ref DataBufferWriter data, SnapshotReceiver receiver, StSnapshotRuntime runtime)
        {
            var state = EntityManager.GetComponentData<TransformState>(entity);
            
            data.WriteValue((half3) state.Position);
            data.WriteValue((half4) state.Rotation.value);
        }

        protected override void ReadDataForEntity(int index, Entity entity, ref DataBufferReader data, SnapshotSender sender, StSnapshotRuntime runtime)
        {
            TransformState state;

            state.Position = data.ReadValue<half3>();
            state.Rotation = (float4) data.ReadValue<half4>();

            if (EntityManager.HasComponent<InterpolationBuffer>(entity))
            {
                var buffer = EntityManager.GetBuffer<InterpolationBuffer>(entity);
                buffer.Add(new InterpolationBuffer(state, runtime.Header.SnapshotIdx, runtime.Header.GameTime.Tick));

                var interData = EntityManager.GetComponentData<InterpolationData>(entity);
                interData.Instance = sender.Client;
                EntityManager.SetComponentData<InterpolationData>(entity, interData);
                
                return;
            }

            EntityManager.SetComponentData(entity, state);
        }
    }
}