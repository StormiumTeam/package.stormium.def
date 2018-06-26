using System;
using package.guerro.shared;
using Unity.Entities;
using UnityEngine;

namespace package.stormium.def
{
    [Serializable]
    public struct DefStVelocity : IComponentData
    {
        /// <summary>
        ///     (Read-only) The concrete velocity is only used for visual and debugging
        /// </summary>
        public Vector3 Velocity;

        /*public DefStVelocity(Vector3 concreteVelocity, Vector3 dynamicVelocity = default(Vector3))
        {
            this.ConcreteVelocity = concreteVelocity;
            this.DynamicVelocity = dynamicVelocity;
        }*/
    }

    public class DefStVelocityWrapper : BetterComponentWrapper<DefStVelocity>
    {
    }
}