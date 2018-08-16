using System;
using package.stormiumteam.shared;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace package.stormium.def
{
    [Serializable]
    public struct DefStMvInput : IComponentData
    {
        public float3 RunDirection;
        
        public int Jump;
        public int WallJump;

        [Range(0, 1)]
        public float Dodge;
        public int WallDodge;
    }

    public class DefStMvInputWrapper : BetterComponentWrapper<DefStMvInput>
    {
    }
}