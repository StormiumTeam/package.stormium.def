using System;
using package.guerro.shared;
using Unity.Entities;

namespace package.stormium.def
{
    [Serializable]
    public struct DefStMvAirEnvironnement : IComponentData
    {
        /// <summary>
        /// The base speed of the air control (default '8')
        /// </summary>
        public float BaseSpeed;
        /// <summary>
        /// The force of the air control (default '12.5')
        /// </summary>
        public float Control;
        /// <summary>
        /// The force of the acceleration with the Y axis. (default '0')
        /// </summary>
        public float AccelerationByHighnessForce;
    }

    public class DefStMvAirEnvironnementWrapper : BetterComponentWrapper<DefStMvAirEnvironnement>
    {
        public DefStMvAirEnvironnementWrapper()
        {
            Value = new DefStMvAirEnvironnement()
            {
                BaseSpeed = 8.0f,
                Control   = 12.5f,
                AccelerationByHighnessForce = 0f
            };
        }
    }
}