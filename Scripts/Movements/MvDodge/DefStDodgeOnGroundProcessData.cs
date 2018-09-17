using Unity.Entities;
using Unity.Mathematics;

namespace package.stormium.def.Movements.Data
{
    public struct DefStDodgeOnGroundProcessData : IComponentData
    {
        public float CooldownBeforeNextDodge;
        public float InertieDelta;
        public float StartFlatSpeed;
        public float3 Direction;
        public int IsDodging;

        public DefStDodgeOnGroundProcessData(float cooldownBeforeNextDodge)
        {
            CooldownBeforeNextDodge = cooldownBeforeNextDodge;
            Direction = float3.zero;
            InertieDelta = 0f;
            StartFlatSpeed = 0f;
            IsDodging = 0;
        }
    }
}