using Unity.Entities;
using UnityEngine;

namespace package.stormium.def.Movements.Data
{
    public struct StGravitySettings : IComponentData
    {
        public byte    FlagIsDefault;
        public Vector3 Value;

        public StGravitySettings(bool defaultGravity, Vector3 value)
        {
            FlagIsDefault = (byte) (defaultGravity ? 1 : 0);
            Value         = value;
        }

        public StGravitySettings(bool defaultGravity)
        {
            FlagIsDefault = (byte) (defaultGravity ? 1 : 0);
            Value         = Vector3.zero;
        }

        public StGravitySettings(Vector3 value)
        {
            FlagIsDefault = 0;
            Value         = value;
        }
    }
}