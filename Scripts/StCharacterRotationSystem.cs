using package.stormium.core;
using Unity.Entities;
using UnityEngine;

namespace package.stormium.def
{
    [UpdateBefore(typeof(UpdateRigidbodySystem))]
    public class StCharacterRotationSystem : ComponentSystem
    {
        [Inject] private Group m_Group;

        protected override void OnStartRunning()
        {
        }

        protected override void OnUpdate()
        {
            for (var i = 0; i != m_Group.Length; i++)
            {
                var rotation = m_Group.Transforms[i].rotation;

                rotation *= Quaternion.Euler(new Vector3(0, Input.GetAxisRaw("Mouse X") * 1f, 0));

                m_Group.Transforms[i].rotation = rotation;
            }
        }

        private struct Group
        {
            public ComponentArray<Transform>       Transforms;
            public ComponentDataArray<StCharacter> Characters;

            public readonly int Length;
        }
    }
}