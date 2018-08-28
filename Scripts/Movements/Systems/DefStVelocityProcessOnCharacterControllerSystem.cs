using LiteNetLib.Utils;
using package.stormium.core;
using package.stormiumteam.networking;
using package.stormiumteam.networking.ecs;
using package.stormiumteam.networking.plugins;
using package.stormiumteam.shared;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine.Jobs;

namespace package.stormium.def.Movements.Systems
{
    [UpdateAfter(typeof(PreLateUpdate))]
    public class DefStVelocityProcessOnCharacterControllerSystem : GameComponentSystem
    {
        // -------------------------------------------------------- //
        // Groups
        // -------------------------------------------------------- //
        struct Group
        {
            public ComponentDataArray<DefStVelocity>        VelocityArray;
            public ComponentArray<CharacterControllerMotor> MotorArray;
            public GameObjectArray                          GameObjects;
            public TransformAccessArray                     Transforms;
            public EntityArray                              Entities;

            public readonly int Length;
        }

        [Inject] private Group m_Group;

        // -------------------------------------------------------- //
        // Fields
        // -------------------------------------------------------- //
        [Inject] private PhysicUpdaterSystem m_PhysicUpdaterSystem;

        private int           m_WriterSize;
        private NetDataWriter m_NetDataWriter;

        protected override void OnCreateManager(int capacity)
        {
            base.OnCreateManager(capacity);
            
            m_WriterSize = MessageIdent.HeaderSize + (sizeof(int) * 2) + UnsafeUtility.SizeOf<float3>();
        }

        protected override void OnUpdate()
        {
            if (!GameServerManagement.IsCurrentlyHosting)
                return;

            for (int frameIndex = 0; frameIndex != m_PhysicUpdaterSystem.LastIterationCount; frameIndex++)
            {
                SimulatePhysicStep(m_PhysicUpdaterSystem.LastFixedTimeStep);
                UpdateInjectedComponentGroups();
            }
        }

        private void SimulatePhysicStep(float dt)
        {
            for (int i = 0; i != m_Group.Length; i++)
            {
                var oldVelocity = m_Group.VelocityArray[i].Value;
                var motor       = m_Group.MotorArray[i];
                var gameObject  = m_Group.GameObjects[i];
                var transform   = m_Group.Transforms[i];
                
                if (!motor.IsGrounded())
                    oldVelocity += new Vector3(0, -10, 0) * dt;
                    
                var wasGrounded = motor.IsGrounded();
                var oldPos      = transform.position;
                var velocity    = oldVelocity * dt;

                var ev = motor.MoveBy(velocity);

                var correctVelocity = oldVelocity;

                for (var j = ev.EventsStartIndex; j < ev.EventsLength; j++)
                {
                    var hitEvent = ev.GetColliderHit(j);
                    if (OnControllerHasHitACollider(hitEvent, velocity.magnitude, oldPos, motor, transform,
                        ref correctVelocity))
                        break;
                }

                // TODO: We have some problem with this (probe don't work well, and sometime we get tped even if we shouldn't)
                /*if (wasGrounded && !motor.IsGrounded()
                                && correctVelocity.y >= -0.5f && correctVelocity.y <= 0
                                && correctVelocity.ToGrid(1).magnitude < 11f)
                    ProbeGround(motor, transform);*/

                m_Group.VelocityArray[i] = new DefStVelocity(correctVelocity);
            }
        }

        private Vector3 GetSlide(Vector3 moveDir, Vector3 normal)
        {
            return moveDir - Vector3.Dot(moveDir, normal) * normal;
        }

        private bool OnControllerHasHitACollider(ControllerColliderHit    hit,
                                                 float                    plannedDistance,
                                                 Vector3                  oldPos,
                                                 CharacterControllerMotor motor,
                                                 Transform                transform,
                                                 ref Vector3              correctedVelocity)
        {
            var controller  = motor.CharacterController;
            var worldCenter = transform.position + controller.center;
            var lowPoint    = worldCenter - new Vector3(0, controller.height * 0.5f, 0);
            var highPoint   = lowPoint + new Vector3(0, controller.height, 0);

            var angle = Vector3.Angle(hit.normal, Vector3.down);

            if (hit.point.y > lowPoint.y + controller.stepOffset)
            {
                var flatVelocity = correctedVelocity.ToGrid(1);
                var flatNormal   = hit.normal.ToGrid(1);

                var undesiredMotion = flatNormal * Vector3.Dot(flatVelocity, flatNormal);
                var desiredMotion   = flatVelocity - undesiredMotion;
                var desiredY        = desiredMotion.y;

                desiredMotion.y = 0;

                desiredMotion = Vector3.ClampMagnitude(desiredMotion, flatVelocity.magnitude);

                desiredMotion.y   = correctedVelocity.y;
                correctedVelocity = desiredMotion;

                // Floor
                if ((controller.collisionFlags == CollisionFlags.Above
                     || (int) controller.collisionFlags == 3)
                    && angle < 90f && correctedVelocity.y > 0)
                    correctedVelocity.y = desiredY;

                return true;
            }

            return false;
        }

        private void ProbeGround(CharacterControllerMotor motor, Transform transform)
        {
            var controller = motor.CharacterController;

            var worldCenter = transform.position + controller.center;
            var lowPoint    = worldCenter - new Vector3(0, controller.height * 0.5f, 0);
            var highPoint   = lowPoint + new Vector3(0, controller.height, 0);

            // Check if we can go back bottom
            var lowestPoint = transform.position - new Vector3(0, controller.height * 0.5f, 0);

            var raycasts = Physics.RaycastAll(lowestPoint, Vector3.down, controller.radius);
            foreach (var ray in raycasts)
            {
                if (ray.transform == transform)
                    continue;

                var velocityToAdd = ray.point - lowestPoint;
                velocityToAdd.y += controller.skinWidth;

                motor.MoveBy(velocityToAdd);

                motor.IsGroundForcedThisFrame = true;
            }
        }
    }
}