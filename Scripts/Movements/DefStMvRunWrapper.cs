using System;
using package.guerro.shared;
using Unity.Entities;

namespace package.stormium.def
{
    public interface IMovement
    {
        
    }
    
    [Serializable]
    public struct DefStMvRun : IComponentData, IMovement
    {
        public float Acceleration;
        public float Deacceleration;
        public float AirAcceleration;
    }

    public class DefStMvRunWrapper : BetterComponentWrapper<DefStMvRun>
    {
        public DefStMvRunWrapper()
        {
            Value = new DefStMvRun
            {
                Acceleration    = 15f,
                Deacceleration  = 12f,
                AirAcceleration = 4f,
            };
        }
    }
}