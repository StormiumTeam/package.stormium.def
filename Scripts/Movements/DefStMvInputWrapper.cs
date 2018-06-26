using System;
using package.guerro.shared;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace package.stormium.def
{
    [Serializable]
    public struct DefStMvInput : IComponentData
    {
        public float3 RunDirection;

        /// <summary>
        ///     Range of [0..1]
        /// </summary>
        [Range(0, 1)]
        public float Jump;
        public int WallJump;

        [Range(0, 1)]
        public float Dodge;
        public int WallDodge;
    }

    public class DefStMvInputWrapper : BetterComponentWrapper<DefStMvInput>
    {
    }
}