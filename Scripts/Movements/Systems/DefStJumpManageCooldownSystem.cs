using package.stormium.def.Movements.Data;
using Unity.Entities;
using UnityEngine;

namespace package.stormium.def.Movements.Systems
{
    [UpdateAfter(typeof(DefStJumpManageInputSystem))]
    public class DefStJumpManageCooldownSystem : ComponentSystem
    {
        private struct Group
        {
            public ComponentDataArray<DefStJumpCooldown> CooldownArray;
            public EntityArray Entities;
            
            public readonly int Length;
        }
        [Inject] private Group m_Group;
        
        protected override void OnUpdate()
        {
            var dt = Time.deltaTime;
            for (int i = 0; i != m_Group.Length; i++)
            {
                var cooldownData = m_Group.CooldownArray[i];
                var entity = m_Group.Entities[i];
                
                cooldownData.Reduce(dt);
                
                if (cooldownData.Value > 0f) PostUpdateCommands.SetComponent(entity, cooldownData);
                else PostUpdateCommands.RemoveComponent<DefStJumpCooldown>(entity);
            }
        }
    }
}