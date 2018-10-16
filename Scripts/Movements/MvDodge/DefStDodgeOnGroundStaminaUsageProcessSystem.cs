using package.stormium.core;
using package.stormium.def;
using package.stormium.def.Movements;
using package.stormium.def.Movements.Data;
using package.stormium.def.Movements.Systems;
using package.stormiumteam.shared;
using Scripts.Movements.MvJump;
using Unity.Entities;
using UnityEngine;

namespace Scripts.Movements
{
    [UpdateAfter(typeof(DefStDodgeOnGroundProcessSystem))]
    public class DefStDodgeOnGroundStaminaUsageProcessSystem : GameComponentSystem,
                                                               StEventDiffuseCommand.IEv
    {
        struct Group
        {
            public ComponentDataArray<DefStDodgeEvent> Events;

            public readonly int Length;
        }

        [Inject] private Group          m_Group;
        [Inject] private AppEventSystem m_AppEventSystem;

        protected override void OnCreateManager()
        {
            m_AppEventSystem.SubscribeToAll(this);
        }

        protected override void OnUpdate()
        {
            if (!GameServerManagement.IsCurrentlyHosting)
                return;
            
            for (var i = 0; i != m_Group.Length; i++)
            {
                var ev = m_Group.Events[i];

                if (!EntityManager.Exists(ev.ServerTarget)) continue;

                var entity = ev.ServerTarget;

                if (!EntityManager.HasComponent<StStamina>(entity) || !EntityManager.HasComponent<DefStDodgeStaminaUsageData>(entity)) continue;

                var staminaComponent = EntityManager.GetComponentData<StStamina>(entity);
                var usageComponent   = EntityManager.GetComponentData<DefStDodgeStaminaUsageData>(entity);

                staminaComponent.Value -= usageComponent.BaseRemove;

                Debug.Log("Updated stamina usage (from dodge) -= " + usageComponent.BaseRemove);

                PostUpdateCommands.SetComponent(entity, staminaComponent);
            }
        }

        public void OnCommandDiffuse(StEventDiffuseCommand.Arguments args)
        {
            if (EntityManager.GetComponentData<EntityCommand>(args.Cmd).GetHeaderType() != typeof(CmdMovement)
                || !EntityManager.HasComponent<CmdMvDodge>(args.Cmd)
                || !EntityManager.HasComponent<EntityCommandTarget>(args.Cmd))
                return;

            var target = EntityManager.GetComponentData<EntityCommandTarget>(args.Cmd).Value;

            if (args.CmdState == CmdState.Begin || args.CmdState == CmdState.Simple)
            {
                // Get stamina usage
                if (EntityManager.HasComponent<StStamina>(target)
                    && EntityManager.HasComponent<DefStDodgeStaminaUsageData>(target))
                {
                    var staminaComponent = EntityManager.GetComponentData<StStamina>(target);
                    var usageComponent = EntityManager.GetComponentData<DefStDodgeStaminaUsageData>(target);

                    if (args.CmdResult.GetComponentData<EntityCommandResult>().AsBool())
                        Debug.Log($"{staminaComponent.Value} < {usageComponent.Needed}");
                    
                    if (usageComponent.Usage == EStaminaUsage.BlockAction && staminaComponent.Value < usageComponent.Needed) 
                        args.SetResult(false);
                }
            }
        }
    }
}