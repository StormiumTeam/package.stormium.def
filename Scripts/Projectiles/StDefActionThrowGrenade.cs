using Unity.Entities;

namespace package.stormium.def.actions
{
    public struct StDefActionThrowGrenade : IComponentData
    {
        public float Speed;
        public int   Damage;
        public int   MaxBounce;

        public StDefActionThrowGrenade(float speed, int damage, int maxBounce)
        {
            Speed  = speed;
            Damage = damage;
            MaxBounce = maxBounce;
        }
    }
}