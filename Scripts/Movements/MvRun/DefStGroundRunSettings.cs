using System;
using Unity.Entities;

namespace package.stormium.def
{
    [Flags]
    public enum GroundFallDamage
    {
        None = 0,
        LoseSpeed = 1,
        LoseStamina = 2
    }
    
    [Serializable]
    public struct DefStGroundRunSettings : IComponentData
    {
        /// <summary>
        ///  The minimal speed for friction to begin lowering (Recommanded Value: 10f)
        /// </summary>
        public float FrictionSpeedMin;
        /// <summary>
        ///  The maximal speed for friction to stop lowering (Recommanded Value: 20f)
        /// </summary>
        public float FrictionSpeedMax;

        /// <summary>
        ///  The minimal friction time (Recommanded Value: 0.25f)
        /// </summary>
        public float FrictionMin;
        /// <summary>
        ///  The maximal friction time (Recommanded Value: 1f)
        /// </summary>
        public float FrictionMax;

        /// <summary>
        ///  The default friction on a surface (Recommanded Value: 6f)
        /// </summary>
        public float SurfaceFriction;

        /// <summary>
        ///  The acceleration speed (Recommanded value: 0.2f)
        /// </summary>
        public float Acceleration;
        /// <summary>
        /// The deacceleration speed (Recommanded value: 0.2f)
        /// </summary>
        public float Deacceleration;
        /// <summary>
        /// The base speed (Recommanded value: 9f)
        /// </summary>
        public float BaseSpeed;

        public GroundFallDamage FallDamage;

        public static DefStGroundRunSettings NewBase()
        {
            return new DefStGroundRunSettings
            {
                FrictionSpeedMin = 10f,
                FrictionSpeedMax = 25f,

                FrictionMin = 0.25f,
                FrictionMax = 1f,

                SurfaceFriction = 6f,

                Acceleration   = 40f,
                Deacceleration = 10f,
                BaseSpeed      = 9.25f,
                
                FallDamage = GroundFallDamage.LoseSpeed
            };
        }
    }
}