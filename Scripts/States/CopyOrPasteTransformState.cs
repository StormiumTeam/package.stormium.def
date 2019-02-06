using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Stormium.Default.States
{
    public class CopyOrPasteTransformState : ComponentSystem
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
                else if (direction.Value == Dir.ConvertFromState)
                {
                    SetPositionAndRotation(entity, state.Position, state.Rotation);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            });
        }

        private void GetPositionAndRotation(Entity entity, out float3 position, out quaternion rotation)
        {
            var hasTransform = EntityManager.HasComponent<Transform>(entity);
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

        private void SetPositionAndRotation(Entity entity, float3 position, quaternion rotation)
        {
            var hasTransform        = EntityManager.HasComponent<Transform>(entity);
            var hasCopyToGameObject = hasTransform && EntityManager.HasComponent<CopyTransformToGameObject>(entity);
            if (hasTransform)
            {
                if (hasCopyToGameObject)
                {
                    EntityManager.SetComponentData(entity, new Position{Value = position});
                    EntityManager.SetComponentData(entity, new Rotation{Value = rotation});
                    
                    return;
                }
                
                //Debug.Log($"{position} {rotation}");

                var tr = EntityManager.GetComponentObject<Transform>(entity);
                tr.position = position;
                tr.rotation = rotation;

                return;
            }
            
            EntityManager.SetComponentData(entity, new Position{Value = position});
            EntityManager.SetComponentData(entity, new Rotation{Value = rotation});
        }
    }
}