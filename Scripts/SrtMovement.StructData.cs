using package.stormiumteam.shared;
using Unity.Mathematics;
using UnityEngine;

namespace package.stormium.def
{
    public struct SrtGroundSettings
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

        public float DecayBaseSpeedFriction;

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

        /// <summary>
        /// The sprint speed (Recommanded value: 12f)
        /// </summary>
        public float SprintSpeed;

        public static SrtGroundSettings NewBase()
        {
            return new SrtGroundSettings
            {
                FrictionSpeedMin = 12f,
                FrictionSpeedMax = 25f,

                FrictionMin = 0.25f,
                FrictionMax = 1f,

                SurfaceFriction = 60f,
                DecayBaseSpeedFriction = 10f,

                Acceleration   = 75f,
                Deacceleration = 20f,
                BaseSpeed      = 8f,
                SprintSpeed    = 12f,
            };
        }
    }
    
    public struct SrtAerialSettings
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
        
        public static SrtAerialSettings NewBase()
        {
            return new SrtAerialSettings
            {
                Acceleration             = 1f,
                AccelerationByHighsForce = 0f,
                Control                  = 35f,
                BaseSpeed                = 9f,
            };
        }
    }
}