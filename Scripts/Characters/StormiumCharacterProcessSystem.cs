using package.stormiumteam.shared;
using Scripts.Movements.Data;
using Scripts.Movements.MvWallBounce;
using Unity.Entities;
using UnityEngine;
using static Unity.Mathematics.math;

namespace package.stormium.def.characters
{
    public class StormiumCharacterProcessSystem : GameComponentSystem
    {
        public struct CharacterGroup
        {
            public ComponentDataArray<StormiumCharacterMvData>        DataArray;
            public ComponentDataArray<StormiumCharacterMvProcessData> ProcessArray;
            public ComponentDataArray<CharacterControllerState>       StateArray;
            public ComponentDataArray<DefStAerialRunSettings>         AerialRunSettingsArray;
            public ComponentDataArray<StVelocity> VelocityArray;

            public EntityArray EntityArray;

            public readonly int Length;
        }

        public struct WallJumpEvent
        {
            public          ComponentDataArray<DefStWallJumpEvent> Events;
            public readonly int                                    Length;
        }
        
        public struct WallDodgeEvent
        {
            public          ComponentDataArray<DefStWallDodgeEvent> Events;
            public readonly int                                    Length;
        }

        [Inject]         CharacterGroup m_CharacterGroup;
        [Inject] private WallJumpEvent  m_WallJumpEvent;
        [Inject] private WallDodgeEvent  m_WallDodgeEvent;

        private ComponentType m_TypeStormiumCharacterMvData;
        private ComponentType m_TypeStormiumCharacterMvProcessData;

        protected override void OnCreateManager()
        {
            m_TypeStormiumCharacterMvData        = ComponentType.Create<StormiumCharacterMvData>();
            m_TypeStormiumCharacterMvProcessData = ComponentType.Create<StormiumCharacterMvProcessData>();
        }

        protected override void OnUpdate()
        {
            if (!CanExecuteServerActions)
                return;
            
            var dt = Time.deltaTime;

            for (var i = 0; i != m_CharacterGroup.Length; i++)
            {
                var data    = m_CharacterGroup.DataArray[i];
                var process = m_CharacterGroup.ProcessArray[i];
                var state   = m_CharacterGroup.StateArray[i];
                var velocity = m_CharacterGroup.VelocityArray[i];
                
                var wasGrounded = process.PrevGroundFlags == 1 && state.GroundFlags != 0;
                var wasInAir = process.PrevGroundFlags == 0 && state.GroundFlags == 1;
                if (wasGrounded)
                {
                    process.AirControlScale = 1f;
                }
                else if (wasInAir)
                {
                    var currSpeed = velocity.Value.ToGrid(1).magnitude;
                    var currY = velocity.Value.y;
                    
                    var speedToRemove = clamp((-process.PrevVelocity.y - 25) * 0.5f, 0, currSpeed * 0.5f);
                    speedToRemove = 0f;
                    
                    Debug.Log(speedToRemove);

                    velocity.Value = velocity.Value.normalized * max(currSpeed - speedToRemove, 0);
                    velocity.Value.y = currY;
                }
                
                if (process.PrevGroundFlags == 0 && state.GroundFlags == 0)
                {
                    process.AirControlScale = clamp(process.AirControlScale - (dt * 0.25f), 0, 1);
                }

                if (Input.GetKeyDown(KeyCode.F))
                {
                    velocity.Value += velocity.Value.ToGrid(1).normalized * 5f;
                    velocity.Value.y += 5f;
                }

                process.PrevGroundFlags = state.GroundFlags;
                process.PrevVelocity = velocity.Value;

                m_CharacterGroup.ProcessArray[i] = process;
                m_CharacterGroup.AerialRunSettingsArray[i] = new DefStAerialRunSettings
                {
                    Acceleration             = 1f,
                    AccelerationByHighsForce = 0.0025f,
                    BaseSpeed                = data.BaseSpeed,
                    Control                  = lerp(data.AirControl, data.AirControlGroundStart, process.AirControlScale)
                };
                m_CharacterGroup.VelocityArray[i] = velocity;
            }

            for (var i = 0; i != m_WallJumpEvent.Length; i++)
            {
                var ev     = m_WallJumpEvent.Events[i];
                var entity = GetEntity(ev.ServerTarget);

                if (!EntityManager.HasComponent(entity, m_TypeStormiumCharacterMvData) ||
                    !EntityManager.HasComponent(entity, m_TypeStormiumCharacterMvProcessData)) continue;

                var processData = EntityManager.GetComponentData<StormiumCharacterMvProcessData>(entity);
                processData.AirControlScale = 0f;

                EntityManager.SetComponentData(entity, processData);
            }
            
            for (var i = 0; i != m_WallDodgeEvent.Length; i++)
            {
                var ev     = m_WallDodgeEvent.Events[i];
                var entity = GetEntity(ev.ServerTarget);

                if (!EntityManager.HasComponent(entity, m_TypeStormiumCharacterMvData) ||
                    !EntityManager.HasComponent(entity, m_TypeStormiumCharacterMvProcessData)) continue;

                var processData = EntityManager.GetComponentData<StormiumCharacterMvProcessData>(entity);
                processData.AirControlScale = 0f;

                EntityManager.SetComponentData(entity, processData);
            }
        }
    }
}