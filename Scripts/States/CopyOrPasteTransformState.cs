using System;
using Stormium.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Stormium.Default.States
{
    public abstract class BaseCopyOrPasteTransformState : ComponentSystem
    {
        internal void GetPositionAndRotation(Entity entity, out float3 position, out quaternion rotation)
        {
            var hasTransform        = EntityManager.HasComponent<Transform>(entity);
            var hasCopyToGameObject = hasTransform && EntityManager.HasComponent<CopyTransformToGameObject>(entity);
            if (hasTransform)
            {
                if (hasCopyToGameObject)
                {
                    position = EntityManager.GetComponentData<Position>(entity).Value;
                    rotation = EntityManager.GetComponentData<Rotation>(entity).Value;

                    return;
                }

                var tr = EntityManager.GetComponentObject<Transform>(entity);

                position = tr.position;
                rotation = tr.rotation;

                return;
            }

            position = EntityManager.GetComponentData<Position>(entity).Value;
            rotation = EntityManager.GetComponentData<Rotation>(entity).Value;
        }

        internal void SetPositionAndRotation(Entity entity, float3 position, quaternion rotation)
        {
            var hasTransform        = EntityManager.HasComponent<Transform>(entity);
            var hasCopyToGameObject = hasTransform && EntityManager.HasComponent<CopyTransformToGameObject>(entity);
            if (hasTransform)
            {
                if (hasCopyToGameObject)
                {
                    EntityManager.SetComponentData(entity, new Position {Value = position});
                    EntityManager.SetComponentData(entity, new Rotation {Value = rotation});

                    return;
                }

                //Debug.Log($"{position} {rotation}");

                var tr = EntityManager.GetComponentObject<Transform>(entity);
                tr.position = position;
                tr.rotation = rotation;

                return;
            }

            EntityManager.SetComponentData(entity, new Position {Value = position});
            EntityManager.SetComponentData(entity, new Rotation {Value = rotation});
        }
    }

    [UpdateInGroup(typeof(STUpdateOrder.UO_BeginData))]
    public class CopyTransformState : BaseCopyOrPasteTransformState
    {
        protected override void OnUpdate()
        {
            ForEach((Entity entity, ref TransformState state, ref TransformStateDirection direction) =>
            {
                if (direction.Value == Dir.ConvertFromState)
                {
                    SetPositionAndRotation(entity, state.Position, state.Rotation);
                }
            });
        }
    }
    
    [UpdateInGroup(typeof(STUpdateOrder.UO_FinalizeData))]
    public class PasteTransformState : BaseCopyOrPasteTransformState
    {
        protected override void OnUpdate()
        {
            ForEach((Entity entity, ref TransformState state, ref TransformStateDirection direction) =>
            {
                if (direction.Value == Dir.ConvertToState)
                {
                    GetPositionAndRotation(entity, out var position, out var rotation);

                    state.Position = position;
                    state.Rotation = rotation;
                }
            });
        }
    }
}