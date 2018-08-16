using package.stormiumteam.shared;
using package.stormium.core;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace package.stormium.def
{
    [UpdateAfter(typeof(Lol))]
    public class StDefaultCharacterCameraSystem : ComponentSystem
    {
        [Inject] private Group m_Group;

        protected override void OnCreateManager(int capacity)
        {
            //UpdateRigidbodySystem.OnAfterSimulate += UpdateRigidbodySystemOnOnAfterSimulate;
        }

        private void UpdateRigidbodySystemOnOnAfterSimulate()
        {
            if (Application.isFocused && Input.GetMouseButtonDown(1)
                                      && Cursor.lockState != CursorLockMode.Locked)
                Cursor.lockState = CursorLockMode.Locked;

            if (Input.GetKeyDown(KeyCode.Escape))
                Cursor.lockState = CursorLockMode.None;

            for (var i = 0; i != m_Group.Length; i++)
            {
                var transform = m_Group.Transforms[i];
                var target    = m_Group.Targets[i];
                var head      = m_Group.Heads[i];
                var info      = m_Group.Informations[i];
                var velocity = m_Group.Velocities[i].Velocity;
                var flatVelocity = velocity.ToGrid(1);

                var pos = transform.position;
                var posY = target.Position.y;

                /*var lerpT = Time.deltaTime * 1.25f + Mathf.Min(Mathf.Abs(pos.y - posY) * 3f, 0.8f);
                lerpT *= 0.5f;
                if (!transform.GetComponent<CharacterControllerMotor>().IsGrounded())
                    lerpT = Mathf.Max(lerpT * 10, Time.deltaTime * 10);
                
                Debug.Log(lerpT);
                    
                posY = Mathf.Lerp(posY, pos.y, lerpT);*/

                // It's somewhat a stair, so we need to step our values
                if (transform.GetComponent<CharacterControllerMotor>().IsGrounded())
                {
                    var obs = Mathf.Abs(pos.y - posY);
                    var abs = obs * 100;
                    abs *= abs;

                    var yChange = Mathf.Max(Mathf.Min(1 - obs, 1) * 1.75f, 1);
                    
                    pos.y = StMath.MoveTorward(posY, pos.y, Time.deltaTime * (abs * yChange * 0.01f));
                }

                //pos.y = posY;
                
                target.Position    = pos;
                target.Rotation    = transform.rotation.eulerAngles;
                
                var distance = StMath.Distance(target.FieldOfView, 70f + Mathf.Clamp(flatVelocity.magnitude, 6, 20) * 0.6f);
                distance = Mathf.Max(0.25f, distance);
                
                target.FieldOfView = StMath.MoveTorward(target.FieldOfView, 70f + (Mathf.Clamp(flatVelocity.magnitude, 6, 20) * 0.6f),
                    Time.deltaTime * distance * 10);

                // Todo: move that
                head.RotationY += Input.GetAxisRaw("Mouse Y") * 0.9f;
                head.RotationY =  math.clamp(head.RotationY, -90, 90);

                target.PositionOffset = head.Position;
                target.RotationOffset = Vector3.left * head.RotationY;

                var velocityLocal = transform.InverseTransformDirection(velocity);
                var roll = velocityLocal.x;
                velocityLocal.x = velocityLocal.x > 0 ? velocityLocal.x * velocityLocal.x
                    : -(velocityLocal.x * velocityLocal.x);
                
                target.RotationOffset.z -= (velocityLocal.x) * 0.01f;
                
                m_Group.Heads[i]   = head;
                m_Group.Targets[i] = target;
            }

            World.Active.GetExistingManager<StCharacterCameraSystem>().Update();
            World.Active.GetExistingManager<STCameraManager>().Update();
        }

        protected override void OnUpdate()
        {
            UpdateRigidbodySystemOnOnAfterSimulate();
            return;

            for (var i = 0; i != m_Group.Length; i++)
            {
                var transform = m_Group.Transforms[i];
                var target    = m_Group.Targets[i];
                var info      = m_Group.Informations[i];

                target.Position    = transform.position + new Vector3(0, 1.6f, 0);
                target.Rotation    = transform.rotation.eulerAngles;
                target.FieldOfView = 70;

                m_Group.Targets[i] = target;
            }

            World.Active.GetExistingManager<StCharacterCameraSystem>().Update();
            World.Active.GetExistingManager<STCameraManager>().Update();
        }

        private struct Group
        {
            public TransformAccessArray                   Transforms;
            public ComponentDataArray<StCharacter>        Characters;
            public ComponentDataArray<DefStCharacterHead> Heads;
            public ComponentDataArray<CameraTargetData>   Targets;
            public ComponentDataArray<DefStMvInformation> Informations;
            public ComponentDataArray<DefStVelocity> Velocities;

            public readonly int Length;
        }
    }
}