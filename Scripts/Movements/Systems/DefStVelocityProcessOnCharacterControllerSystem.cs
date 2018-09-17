using System;
using System.Collections.Generic;
using LiteNetLib.Utils;
using package.stormium.core;
using package.stormiumteam.networking;
using package.stormiumteam.networking.ecs;
using package.stormiumteam.networking.plugins;
using package.stormiumteam.shared;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine.Jobs;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

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
            public ComponentDataArray<StVelocity>        VelocityArray;
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

        private static RaycastHit[] s_RaycastHits;
        private static NativeArray<Vector3> s_RepairNormalResult;

        private int           m_WriterSize;
        private NetDataWriter m_NetDataWriter;

        protected override void OnCreateManager(int capacity)
        {
            base.OnCreateManager(capacity);
            
            m_WriterSize = MessageIdent.HeaderSize + (sizeof(int) * 2) + UnsafeUtility.SizeOf<float3>();
            s_RaycastHits = new RaycastHit[64];
            s_RepairNormalResult = new NativeArray<Vector3>(1, Allocator.Persistent);
        }

        protected override void OnUpdate()
        {
            if (!GameServerManagement.IsCurrentlyHosting && IsConnectedOrHosting)
                return;

            for (int frameIndex = 0; frameIndex != m_PhysicUpdaterSystem.LastIterationCount; frameIndex++)
            {
                Profiler.BeginSample("SimulatePhysicStep");
                SimulatePhysicStep(m_PhysicUpdaterSystem.LastFixedTimeStep);
                Profiler.EndSample();
                Profiler.BeginSample("UpdateInjectedComponentGroups");
                UpdateInjectedComponentGroups();
                Profiler.EndSample();
            }

            if (IsConnectedOrHosting)
                return;

            // TODO: HACK REMOVE THIS
            var i           = m_Group.Length - 1;
            var pos         = m_Group.Transforms[i].position;
            var firstCamera = Object.FindObjectOfType<Camera>();

            pos.y += 1.6f;

            firstCamera.transform.position = pos;
            firstCamera.transform.rotation = Quaternion.Euler(m_Group.Entities[i].GetComponentData<DefStEntityAimClientInput>().Aim);
        }

        private void SimulatePhysicStep(float dt)
        {
            for (int i = 0; i != m_Group.Length; i++)
            {
                var oldVelocity = m_Group.VelocityArray[i].Value;
                var motor       = m_Group.MotorArray[i];
                var gameObject  = m_Group.GameObjects[i];
                var transform   = m_Group.Transforms[i];

                if (!motor.IsGrounded() && !motor.IsStableOnGround && !motor.IsSliding)
                {
                    oldVelocity += new Vector3(0, Physics.gravity.y, 0) * dt;
                    motor.CharacterController.stepOffset = 0.05f;
                }
                else if (motor.IsGrounded() && motor.IsStableOnGround && !motor.IsSliding)
                {
                    motor.CharacterController.stepOffset = 0.5f;
                    oldVelocity.y = -motor.CharacterController.stepOffset;
                }

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
                
                // Slide on floor (done next frame)
                var slideAngle = Vector3.Angle(motor.AngleDir, Vector3.up);
                if (slideAngle > 45 && slideAngle < 89 && motor.IsGrounded())
                {                                      
                    var oldY = correctVelocity.y;
                    
                    var undesiredMotion = motor.AngleDir * Vector3.Dot(correctVelocity, motor.AngleDir);
                    var desiredMotion   = correctVelocity - undesiredMotion;
                    var leftOverInertia = desiredMotion * 0.5f * dt;
                    
                    correctVelocity   = desiredMotion + leftOverInertia;
                    correctVelocity.y = oldY - 15 * dt;
                    correctVelocity.y = Mathf.Clamp(correctVelocity.y, -10, 60);
                    
                    motor.AngleDir = ProbeGround(motor, transform, motor.AngleDir, 90, 90);
                    
                    motor.IsStableOnGround = false;
                    motor.IsSliding        = true;
                }
                else
                {
                    motor.IsStableOnGround = motor.IsGrounded();
                    motor.IsSliding        = false;
                }

                if (motor.IsGrounded())
                {
                    motor.AngleDir = GetAngleDir(motor, transform);
                }
                else if (wasGrounded && !motor.IsGrounded()
                                     && correctVelocity.y >= -0.5f && correctVelocity.y <= 0)
                {
                    motor.AngleDir = ProbeGround(motor, transform, motor.AngleDir,
                        45 - Mathf.Clamp(correctVelocity.ToGrid(1).magnitude, 0, 15) * 3 + 15, 45);
                }
                
                m_Group.VelocityArray[i] = new StVelocity(correctVelocity);
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

        private Vector3 ProbeGround(CharacterControllerMotor motor, Transform transform, Vector3 previousDirection, float maxAngle, float globalMaxAngle)
        {
            var controller = motor.CharacterController;

            var worldCenter = transform.position + controller.center;
            var lowPoint    = worldCenter - new Vector3(0, controller.height * 0.5f, 0);
            var highPoint   = lowPoint + new Vector3(0, controller.height, 0);

            Profiler.BeginSample("RaycastNonAlloc");
            var rayLength = Physics.RaycastNonAlloc(lowPoint, Vector3.down, s_RaycastHits, controller.stepOffset);
            Profiler.EndSample();
            
            if (previousDirection == Vector3.zero)
                previousDirection = Vector3.up;
            
            var highestAngle = 0f;
            var highestDir = previousDirection;
            for (int i = 0; i != rayLength; i++)
            {
                var ray = s_RaycastHits[i];
                if (ray.transform == transform)
                    continue;
                
                ray.normal = RepairHitSurfaceNormal(ray);
                
                var angle = Vector3.Angle(previousDirection, ray.normal);
                if (angle > highestAngle)
                {
                    highestAngle = angle;
                    highestDir   = ray.normal;
                }

                if (angle > maxAngle || Vector3.Angle(Vector3.up, ray.normal) > globalMaxAngle)
                    continue;
                
                var velocityToAdd = ray.point - lowPoint;
                
                motor.MoveBy(velocityToAdd);

                motor.IsGroundForcedThisFrame = true;
            }

            return highestDir;
        }

        private Vector3 GetAngleDir(CharacterControllerMotor motor, Transform transform)
        {
            CPhysicSettings.Active.SetGlobalCollision(motor.gameObject, false);

            var controller = motor.CharacterController;
            var height     = controller.height;
            var radius     = controller.radius;

            var worldCenter = transform.position + controller.center;
            var lowPoint    = worldCenter - new Vector3(0, height * 0.5f, 0);
            var highPoint   = lowPoint + new Vector3(0, height, 0);
            var distance    = height * 0.25f + 0.1f + (controller.skinWidth * 4);

            var layerMask     = CPhysicSettings.PhysicInteractionLayerMask;
            var rayLength     = Physics.CapsuleCastNonAlloc(worldCenter, highPoint, radius, Vector3.down, s_RaycastHits, distance, layerMask);
            var smallestAngle = float.MaxValue;
            var smallestDir   = Vector3.down;

            if (rayLength > 0) motor.IsGroundForcedThisFrame = true;
            
            for (int i = 0; i != rayLength; i++)
            {
                var ray    = s_RaycastHits[i];
                var normal = RepairHitSurfaceNormal(ray);
                var angle = Vector3.Angle(Vector3.up, normal);

                if (angle < smallestAngle)
                {
                    smallestAngle = angle;
                    smallestDir   = normal;
                }
            }

            CPhysicSettings.Active.SetGlobalCollision(motor.gameObject, true);

            return smallestDir;
        }

        public static Vector3 RepairHitSurfaceNormal(RaycastHit hit)
        {
            var collider = hit.collider;

            var meshCollider = collider as MeshCollider;
            if (meshCollider != null)
            {
                Profiler.BeginSample("Get things");
                var mesh     = meshCollider.sharedMesh;
                var triangles = PhysicMeshTool.GetTriangles(mesh);
                var vertices = PhysicMeshTool.GetVertices(mesh);
                Profiler.EndSample();
                
                var job = new JobRepairHitSurfaceNormal()
                {
                    TriangleIndex = hit.triangleIndex,
                    Triangles    = triangles,
                    Vertices     = vertices,
                    ResultNormal = s_RepairNormalResult
                };
                job.Run();
                Profiler.BeginSample("Get direction");
                var dir = hit.transform.TransformDirection(s_RepairNormalResult[0]);
                Profiler.EndSample();
                return dir;
            }

            return hit.normal;
        }

        [BurstCompile]
        struct JobRepairHitSurfaceNormal : IJob
        {
            public int TriangleIndex;
            public NativeArray<int> Triangles;
            public NativeArray<Vector3> Vertices;

            public NativeArray<Vector3> ResultNormal;
            
            public void Execute()
            {
                var v0 = Vertices[Triangles[TriangleIndex * 3]];
                var v1 = Vertices[Triangles[TriangleIndex * 3 + 1]];
                var v2 = Vertices[Triangles[TriangleIndex * 3 + 2]];

                ResultNormal[0] = Vector3.Cross(v1 - v0, v2 - v1).normalized;
            }
        }
    }
}