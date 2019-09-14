using Stormium.Default.States;
using StormiumTeam.GameBase;
using StormiumTeam.GameBase.Data;
using StormiumTeam.Shared.Gen;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Scripts
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public class UpdateCameraFreeMove : GameBaseSystem
    {
        private EntityQuery m_Query;

        protected override void OnCreate()
        {
            m_Query = GetEntityQuery(typeof(LocalCameraFreeMove), typeof(Translation), typeof(Rotation));
        }

        protected override void OnUpdate()
        {
            var deltaTime = ServerTick.Delta;

            var nMove = math.normalizesafe(new float2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")));

            if (Input.GetKey(KeyCode.LeftShift))
                nMove *= 2.0f;

            var nJet = 0.0f;
            if (Input.GetKey(KeyCode.Space))
                nJet = 1.0f;
            if (Input.GetKey(KeyCode.LeftControl))
                nJet = -1.0f;

            LocalCameraFreeMove freeMove    = default;
            Translation         translation = default;
            Rotation            rotation    = default;

            foreach (var (i, entity) in this.ToEnumerator_DDD(m_Query, ref freeMove, ref translation, ref rotation))
            {
                var move = math.lerp(freeMove.PreviousMove, nMove, deltaTime * 12.5f);
                var jet  = math.lerp(freeMove.PreviousJet, nJet, deltaTime * 20f);

                var horizontal = move.x * freeMove.Intensity * deltaTime;
                var vertical   = move.y * freeMove.Intensity * deltaTime;
                var look       = GetNewAimLook(freeMove.PreviousAimLook);

                rotation.Value    =  Quaternion.Euler(-look.y, look.x, 0.0f);
                translation.Value += math.mul(rotation.Value, new Vector3(horizontal, 0, vertical));

                translation.Value.y += jet * (freeMove.Intensity * 0.75f) * deltaTime;

                if (EntityManager.HasComponent<CameraModifierData>(entity))
                {
                    EntityManager.SetComponentData(entity, new CameraModifierData
                    {
                        FieldOfView = 60.0f,
                        Position    = translation.Value,
                        Rotation    = rotation.Value
                    });
                }

                freeMove.PreviousJet     = jet;
                freeMove.PreviousMove    = move;
                freeMove.PreviousAimLook = look;
            }
        }

        private float2 GetNewAimLook(float2 previous)
        {
            var input = new float2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 1.5f;
                    
            var newRotation = previous + input;
            newRotation.x %= 360;
            newRotation.y = Mathf.Clamp(newRotation.y, -89f, 89f);

            return newRotation;
        }
    }
}