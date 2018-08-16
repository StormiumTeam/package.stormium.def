using package.stormiumteam.shared;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace package.stormium.def
{
    public struct StCharacter : IComponentData
    {
    }

    [RequireComponent(typeof(CharacterController), typeof(CharacterControllerMotor))]
    [RequireComponent(typeof(PositionComponent), typeof(RotationComponent))]
    public class StCharacterWrapper : BetterComponentWrapper<StCharacter>
    {
    }
}