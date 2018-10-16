using package.stormium.core;
using package.stormium.def.actions;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace package.stormium.def.Projectiles
{
    public class StDefProjectileRocketProcessSystem : StProjectileSystem
    {
        protected struct Group
        {
            public ComponentDataArray<ProjectileTag>         ProjectileTag;
            public ComponentDataArray<ProjectileOwner>       OwnerArray;
            public ComponentDataArray<Position> PositionArray;
            public ComponentDataArray<StVelocity>            VelocityArray;
            public ComponentDataArray<StDefProjectileRocket> RocketArray;
            public EntityArray                               Entities;

            public readonly int Length;
        }

        [Inject] private Group m_ProjectilesGroup;
        private RaycastHit[] m_BufferHits;

        protected override void OnCreateManager()
        {
            m_BufferHits = new RaycastHit[64];
        }

        protected override void OnUpdate()
        {
            ProcessPhysics(m_ProjectilesGroup);
        }

        protected void ProcessPhysics(Group group)
        {
            for (int i = 0; i != group.Length; i++)
            {
                var positionData = group.PositionArray[i];
                var velocityData = group.VelocityArray[i];
                var entity = group.Entities[i];
                
                var length = Physics.RaycastNonAlloc(positionData.Value, velocityData.Value.normalized, m_BufferHits);
                for (int bIndex = 0; bIndex != length; bIndex++)
                {
                    var rayHit = m_BufferHits[bIndex];
                }
            }
        }
    }
}