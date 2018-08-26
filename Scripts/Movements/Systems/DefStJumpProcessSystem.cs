using package.stormium.def.Movements.Data;
using package.stormiumteam.shared;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.Input;

namespace package.stormium.def.Movements.Systems
{
    [UpdateAfter(typeof(DefStJumpManageCooldownSystem))]
    public class DefStJumpProcessSystem : ComponentSystem
    {
        struct Group
        {
            public ComponentDataArray<DefStVelocity>                        Velocities;
            public ComponentDataArray<DefStJumpSettings>                    Settings;
            public ComponentDataArray<DefStJumpInput>                       Inputs;
            public ComponentArray<CharacterControllerMotor> Motors;
            public SubtractiveComponent<VoidSystem<DefStJumpProcessSystem>> Void1;
            public EntityArray                                              Entities;

            public readonly int Length;
        }

        [Inject] private Group m_Group;

        private Vector3         m_CachedDefaultGravity;
        private EntityArchetype m_ArchetypeEventOnCharacterJump;

        protected override void OnCreateManager(int capacity)
        {
            m_ArchetypeEventOnCharacterJump = EntityManager.CreateArchetype(typeof(DefStOnCharacterJump));
        }

        protected override void OnUpdate()
        {
            m_CachedDefaultGravity = Physics.gravity;

            for (int i = 0; i != m_Group.Length; i++)
            {
                var entity   = m_Group.Entities[i];
                var velocity = m_Group.Velocities[i];
                var setting  = m_Group.Settings[i];
                var input    = m_Group.Inputs[i];
                var motor = m_Group.Motors[i];

                if (!ProcessItem(ref entity, ref velocity, ref setting, ref input, motor))
                    continue;

                PostUpdateCommands.SetComponent(entity, velocity);
                PostUpdateCommands.SetComponent(setting);
                PostUpdateCommands.SetComponent(input);
            }
        }

        private bool ProcessItem
        (
            ref Entity               entity,
            ref DefStVelocity        velocity,
            ref DefStJumpSettings    setting,
            ref DefStJumpInput       input,
            CharacterControllerMotor motor
        )
        {
            var doJump = input.State != InputState.None;
            if (!doJump)
                return false;

            var gravity = GetGravity(entity, setting);
            
            var doAirJump = input.State == InputState.Down;
            if (motor.IsGrounded())
                velocity.Value += gravity * setting.JumpPower;

            PostUpdateCommands.CreateEntity(m_ArchetypeEventOnCharacterJump);
            PostUpdateCommands.SetComponent(new DefStOnCharacterJump(entity));

            return true;
        }

        private Vector3 GetGravity(Entity entity, DefStJumpSettings setting)
        {
            if (setting.GravityGravityType == GravityType.Custom)
                return setting.Gravity;
            if (setting.GravityGravityType == GravityType.Default)
                return m_CachedDefaultGravity;

            if (EntityManager.HasComponent<StGravitySettings>(entity))
            {
                var data = EntityManager.GetComponentData<StGravitySettings>(entity);
                return data.FlagIsDefault == 1 ? m_CachedDefaultGravity : data.Value;
            }

            return m_CachedDefaultGravity;
        }
    }
}