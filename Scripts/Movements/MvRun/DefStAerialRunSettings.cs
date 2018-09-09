using Unity.Entities;

namespace Scripts.Movements.Data
{
    public struct DefStAerialRunSettings : IComponentData
    {
        /// <summary>
        ///  The acceleration speed (Recommanded value: 14f)
        /// </summary>
        public float Acceleration;
        /// <summary>
        /// The force power [0-1] of the acceleration provoked by the Y axis (Recommanded value: 0.25f)
        /// </summary>
        public float AccelerationByHighsForce;
        
        /// <summary>
        /// The base speed (Recommanded value: 9f)
        /// </summary>
        public float BaseSpeed;
        /// <summary>
        /// The force of the air control (Recommanded value: 12.5f)
        /// </summary>
        public float Control;
        
        public static DefStAerialRunSettings NewBase()
        {
            return new DefStAerialRunSettings
            {
                Acceleration             = 1.25f,
                AccelerationByHighsForce = 0.5f,
                Control                  = 17.5f,
                BaseSpeed                = 9f,
            };
        }
    }
}