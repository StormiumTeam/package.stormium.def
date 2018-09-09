using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace package.stormium.def.Movements.Data
{
    public struct DefStRunClientInput : IComponentData
    {
        public float2 Direction;

        public DefStRunClientInput(float2 direction)
        {
            Direction = direction;
        }
    }
    
    public struct DefStRunInput : IComponentData
    {
        public float Timestamp;
        public float2 Direction;

        public DefStRunInput(float2 direction)
        {
            Timestamp = Time.time;
            Direction = direction;
        }
        
        public DefStRunInput(float timestamp, float2 direction)
        {
            Timestamp = timestamp;
            Direction = direction;
        }
    }
}