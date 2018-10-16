using Unity.Entities;

namespace package.stormium.def.actions
{
    public struct StDefActionRocketLauncher : IComponentData
    {
        public float Speed;
        public int Damage;

        public StDefActionRocketLauncher(float speed, int damage)
        {
            Speed = speed;
            Damage = damage;
        }
    }
}