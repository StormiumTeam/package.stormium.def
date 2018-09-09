using System;
using package.stormiumteam.shared;
using Unity.Entities;
using UnityEngine;

namespace package.stormium.def
{
    [Serializable]
    public struct StVelocity : IComponentData
    {
        /// <summary>
        ///     (Read-only) The concrete velocity is only used for visual and debugging
        /// </summary>
        public Vector3 Value;

        public StVelocity(Vector3 velocity)
        {
            Value = velocity;
        }
    }

    public class DefStVelocityWrapper : BetterComponentWrapper<StVelocity>
    {
    }
}