using System;
using package.guerro.shared;
using Unity.Entities;
using UnityEngine;

namespace package.stormium.def
{
    [Serializable]
    public struct DefStMvGravity : IComponentData
    {
        public enum GravityMode
        {
            Physics,
            Custom
        }

        public GravityMode Mode;
        public Vector3     Gravity;
    }

    public class DefStMvGravityWrapper : BetterComponentWrapper<DefStMvGravity>
    {
        public DefStMvGravityWrapper()
        {
            Value = new DefStMvGravity
            {
                Mode    = DefStMvGravity.GravityMode.Custom,
                //Gravity = new Vector3(0, -19.62f, 0)
                Gravity = new Vector3(0, -16.5f, 0)
            };
        }
    }
}