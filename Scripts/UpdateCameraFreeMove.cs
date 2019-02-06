using Runtime.Data;
using Stormium.Core;
using Stormium.Default.States;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Scripts
{
    public class UpdateCameraFreeMove : ComponentSystem
    {
        private float m_Delta;
        private float2 m_Move;
        private float m_Jet;
        private float2 m_Look;

        protected override void OnUpdate()
        {
            m_Delta = World.GetExistingManager<StGameTimeManager>().GetTimeFromSingleton().DeltaTime;

            var nMove = math.normalizesafe(new float2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")));

            if (Input.GetKey(KeyCode.LeftShift))
                nMove *= 2.0f;
                
            var nJet  = 0.0f;
            if (Input.GetKey(KeyCode.Space))
                nJet = 1.0f;
            if (Input.GetKey(KeyCode.LeftControl))
                nJet = -1.0f;

            m_Move = math.lerp(m_Move, nMove, m_Delta * 12.5f);
            m_Jet  = math.lerp(m_Jet, nJet, m_Delta * 20f);

            m_Look = GetNewAimLook(m_Look);

            ForEach((Entity entity, ref LocalCameraFreeMove freeMove, ref Position position, ref Rotation rotation) =>
            {
                var horizontal = m_Move.x * freeMove.Intensity * m_Delta;
                var vertical   = m_Move.y * freeMove.Intensity * m_Delta;
                var look       = m_Look;

                rotation.Value =  Quaternion.Euler(-look.y, look.x, 0.0f);
                position.Value += math.mul(rotation.Value, new Vector3(horizontal, 0, vertical));

                position.Value.y += m_Jet * (freeMove.Intensity * 0.75f) * m_Delta;

                if (EntityManager.HasComponent<CameraModifierData>(entity))
                {
                    EntityManager.SetComponentData(entity, new CameraModifierData
                    {
                        FieldOfView = 60.0f,
                        Position    = position.Value,
                        Rotation    = rotation.Value
                    });
                }
            });
        }

        private float2 GetNewAimLook(float2 previous)
        {
            var input = new float2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 1.5f;
                    
            var newRotation = previous + input;
            newRotation.x = newRotation.x % 360;
            newRotation.y = Mathf.Clamp(newRotation.y, -89f, 89f);

            return newRotation;
        }
    }
}