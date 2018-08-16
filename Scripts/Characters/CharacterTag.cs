using Unity.Entities;

namespace package.stormium.def.characters
{
    /// <summary>
    /// Tag for character identification
    /// </summary>
    public struct CharacterTag : IComponentData
    {
        
    }

    public struct CharacterPlayerOwner : IComponentData
    {
        public Entity Target;
    }
}