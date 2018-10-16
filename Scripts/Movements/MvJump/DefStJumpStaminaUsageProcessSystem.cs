using package.stormium.def;
using package.stormium.def.Movements;
using package.stormium.def.Movements.Data;
using package.stormium.def.Movements.Systems;
using package.stormiumteam.shared;
using Unity.Entities;
using UnityEngine;

namespace Scripts.Movements.MvJump
{
    [UpdateAfter(typeof(DefStJumpProcessSystem))]
    public class DefStJumpStaminaUsageProcessSystem : GameComponentSystem
    {
        struct Group
        {
            public ComponentDataArray<DefStJumpEvent> Events;

            public readonly int Length;
        }

        [Inject] private Group m_Group;
        
        protected override void OnUpdate()
        {
            for (var i = 0; i != m_Group.Length; i++)
            {
                var ev = m_Group.Events[i];
                
                if (IsConnectedOrHosting ? !ServerEntityMgr.HasEntity(ev.ServerTarget) : !EntityManager.Exists(ev.ServerTarget)) continue;
                
                var entity = IsConnectedOrHosting ? ServerEntityMgr.GetEntity(ev.ServerTarget) : ev.ServerTarget;
                
                if (!EntityManager.HasComponent<StStamina>(entity) || !EntityManager.HasComponent<DefStJumpStaminaUsageData>(entity)) continue;
                
                var staminaComponent = EntityManager.GetComponentData<StStamina>(entity);
                var usageComponent   = EntityManager.GetComponentData<DefStJumpStaminaUsageData>(entity);

                var removal = usageComponent.BaseRemove;
                if (EntityManager.HasComponent<StVelocity>(entity))
                {
                    var flatSpeed = EntityManager.GetComponentData<StVelocity>(entity).Value.ToGrid(1).magnitude;
                    removal += flatSpeed * usageComponent.RemoveBySpeedFactor01;
                }
                        
                staminaComponent.Value -= removal;
                
                Debug.Log("Updated stamina usage (from jump) -= " + removal);
                
                PostUpdateCommands.SetComponent(entity, staminaComponent);
            }
        }
    }
}