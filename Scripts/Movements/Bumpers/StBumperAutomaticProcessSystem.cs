using System;
using package.stormium.def.characters;
using package.stormium.def.Movements.Systems;
using package.stormiumteam.shared;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace package.stormium.def
{
    public class StBumperAutomaticProcessCollisionSystem : GameComponentSystem
    {
        struct CollisionEvents
        {
            public ComponentDataArray<Position>                PositionArray;
            public ComponentDataArray<Rotation>                RotationArray;
            public ComponentDataArray<StBumperPlatformData>    PlatformArray;
            public SharedComponentDataArray<StBumperAutomatic> AutomaticArray;

            public EntityArray Entities;

            public readonly int Length;
        }

        [Inject] private CollisionEvents m_CollisionEvents;

        protected override void OnUpdate()
        {
        }
    }
    
    [UpdateAfter(typeof(DefStVelocityProcessOnCharacterControllerSystem))]
    public class StBumperAutomaticProcessBumpingSystem : GameComponentSystem
    {
        struct Group
        {
            public ComponentDataArray<Position>                PositionArray;
            public ComponentDataArray<Rotation>                RotationArray;
            public ComponentDataArray<StBumperPlatformData>    PlatformArray;
            [ReadOnly] public SharedComponentDataArray<StBumperAutomatic> AutomaticArray;

            public EntityArray Entities;

            public readonly int Length;
        }

        struct CharacterGroup
        {
            public ComponentDataArray<StVelocity> VelocityArray;
            public ComponentArray<CharacterController> CharacterControllers;
            public ComponentDataArray<StormiumCharacterMvProcessData> st;

            public readonly int Length;
        }

        [Inject] private Group m_Group;
        [Inject] private CharacterGroup m_CharacterGroup;

        private float Delay;

        protected override void OnUpdate()
        {
            if (Delay > 0)
            {
                Delay -= Time.deltaTime;
                return;
            }
            
            for (int i = 0; i != m_Group.Length; i++)
            {
                var position = m_Group.PositionArray[i].Value;
                var rotation = m_Group.RotationArray[i].Value;
                var platform = m_Group.PlatformArray[i];
                var collider = m_Group.AutomaticArray[i].TriggerCollider;

                var entity = m_Group.Entities[i];

                for (int j = 0; j != m_CharacterGroup.Length; j++)
                {
                    var velocity = m_CharacterGroup.VelocityArray[j];
                    var character = m_CharacterGroup.CharacterControllers[j];
                    var stch = m_CharacterGroup.st[j];

                    Vector3 direction;
                    float distance;
                    if (Physics.ComputePenetration(collider, position, rotation,
                        character, character.transform.position, character.transform.rotation,
                        out direction, out distance))
                    {
                        if (platform.VelocityType == VelocityType.AddVelocity)
                        {
                            velocity.Value.y =  0f;
                            velocity.Value   += platform.Direction;
                        }
                        else
                        {
                            velocity.Value = platform.Direction;
                        }

                        stch.AirControlScale = 0f;

                        m_CharacterGroup.st[j] = stch;
                    }

                    Delay = 0.1f;
                    
                    m_CharacterGroup.VelocityArray[j] = velocity;
                }
            }
        }
    }
}