using Unity.Entities;

namespace package.stormium.def.Movements.Data
{
    public struct DefStOnCharacterJump : IComponentData
    {
        public Entity Character;

        public DefStOnCharacterJump(Entity character)
        {
            Character = character;
        }
    }
}