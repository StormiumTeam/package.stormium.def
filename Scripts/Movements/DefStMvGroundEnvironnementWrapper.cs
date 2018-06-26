using System;
using package.guerro.shared;
using Unity.Entities;

namespace package.stormium.def
{
    [Serializable]
    public struct DefStMvGroundEnvironnement : IComponentData
    {
        /// <summary>
        /// The Ground base speed (8)
        /// </summary>
        public float BaseSpeed;
        /// <summary>
        /// The minimal speed for starting lowering the friction (6)
        /// </summary>
        public float SpeedFrictionMin;
        /// <summary>
        /// The maximal speed for stopping lowering the friction (32)
        /// </summary>
        public float SpeedFrictionMax;
        /// <summary>
        /// The minimal friction (0.3)
        /// </summary>
        public float FrictionMin;
        /// <summary>
        /// The maximal friction (1)
        /// </summary>
        public float FrictionMax;
        /// <summary>
        /// The ground friction (6)
        /// </summary>
        public float GroundFriction;
    }

    public class DefStMvGroundEnvironnementWrapper : BetterComponentWrapper<DefStMvGroundEnvironnement>
    {
        public DefStMvGroundEnvironnementWrapper()
        {
            Value = new DefStMvGroundEnvironnement()
            {
                BaseSpeed = 8.0f,
                GroundFriction   = 6f,
                SpeedFrictionMin = 6f,
                SpeedFrictionMax = 32f,
                FrictionMin      = 0.3f,
                FrictionMax      = 1f
            };
        }
    }
}