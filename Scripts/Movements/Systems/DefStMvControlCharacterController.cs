using package.guerro.shared;
using package.stormium.core;
using Unity.Entities;
using UnityEngine;

namespace package.stormium.def
{
    [UpdateBefore(typeof(STUpdateOrder.UORigidbodyUpdateBefore))]
    public class DefStMvControlCharacterController : ComponentSystem
    {
        [Inject] private Group m_Group;

        protected override void OnUpdate()
        {
            for (var i = 0; i < m_Group.Length; i++)
            {
                var controller = m_Group.Controllers[i];
                var motor      = m_Group.Motors[i];
            }
        }

        private struct Group
        {
            public ComponentDataArray<StCharacter>          Characters;
            public ComponentArray<CharacterController>      Controllers;
            public ComponentArray<CharacterControllerMotor> Motors;

            public int Length;
        }
    }
}