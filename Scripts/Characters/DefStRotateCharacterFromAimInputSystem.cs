using Unity.Entities;
using UnityEngine;
using UnityEngine.Jobs;

namespace package.stormium.def.characters
{
    public class DefStRotateCharacterFromAimInputSystem : GameComponentSystem
    {
        struct Group
        {
            public ComponentDataArray<DefStEntityAimInput> AimInputArray;
            public TransformAccessArray Transforms;

            public readonly int Length;
        }
        [Inject] private Group m_Group;
        
        protected override void OnUpdate()
        {
            if (!GameServerManagement.IsCurrentlyHosting)
                return;
                
            for (int i = 0; i != m_Group.Length; i++)
            {
                m_Group.Transforms[i].rotation = Quaternion.Euler(0, m_Group.AimInputArray[i].Aim.y, 0);
            }
        }
    }
}