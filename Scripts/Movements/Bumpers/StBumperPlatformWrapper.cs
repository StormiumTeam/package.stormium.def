using System;
using package.stormiumteam.shared;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

namespace package.stormium.def
{
    public enum VelocityType
    {
        AddVelocity,
        SetVelocity,
    }
    
    [Serializable]
    public struct StBumperPlatformData : IComponentData
    {
        public VelocityType VelocityType;
        public Vector3 Direction;
    }

    [RequireComponent(typeof(PositionComponent), typeof(RotationComponent))]
    public class StBumperPlatformWrapper : ComponentDataWrapper<StBumperPlatformData>
    {
        private void OnDrawGizmos()
        {
            DrawGizmos(false);
        }

        private void OnDrawGizmosSelected()
        {
            DrawGizmos(true);
        }

        private void DrawGizmos(bool selected)
        {
            Gizmos.color = selected ? Color.yellow : Color.white;

            var gameObjectEntity = GetComponent<GameObjectEntity>();
            var entityManager = gameObjectEntity.EntityManager;
            var entity = gameObjectEntity.Entity;
            
            var direction = (Vector3) math.mul(transform.rotation, Value.Direction);
            
            Gizmos.DrawRay(transform.position, direction + (Physics.gravity * 0.5f));

            var automaticComponent = GetComponent<StBumperAutomaticWrapper>();
            if (automaticComponent != null && automaticComponent.Value.TriggerCollider != null)
            {
                var trigger = automaticComponent.Value.TriggerCollider;
                
                Gizmos.DrawWireCube(trigger.bounds.center, trigger.bounds.size);
            }
        }
    }
}