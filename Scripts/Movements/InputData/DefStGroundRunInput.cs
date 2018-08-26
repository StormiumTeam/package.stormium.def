using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace package.stormium.def.Movements.Data
{
    public struct DefStGroundRunInput : IComponentData
    {
        public float Timestamp;
        public float2 Direction;

        public DefStGroundRunInput(float2 direction)
        {
            Timestamp = Time.time;
            Direction = direction;
        }
        
        public DefStGroundRunInput(float timestamp, float2 direction)
        {
            Timestamp = timestamp;
            Direction = direction;
        }
    }
}