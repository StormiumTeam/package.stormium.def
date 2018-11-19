using System;
using System.Collections.Generic;
using LiteNetLib.Utils;
using package.stormium.core;
using package.stormium.def.Utilities;
using package.stormiumteam.networking;
using package.stormiumteam.networking.ecs;
using package.stormiumteam.networking.plugins;
using package.stormiumteam.shared;
using Scripts;
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
            public ComponentDataArray<CharacterControllerState> States;
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
        private CPhysicGroup m_CharacterGroup;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();
            
            m_WriterSize = MessageIdent.HeaderSize + (sizeof(int) * 2) + UnsafeUtility.SizeOf<float3>();
            s_RaycastHits = new RaycastHit[64];
            s_RepairNormalResult = new NativeArray<Vector3>(1, Allocator.Persistent);
        }

        protected override void OnDestroyManager()
        {
            s_RepairNormalResult.Dispose();
        }

        protected override void OnUpdate()
        {
            if (!GameServerManagement.IsCurrentlyHosting && IsConnectedOrHosting)
                return;

            m_CharacterGroup = KnowPhysicGroups.CharacterGroup;
            
            // Because we are going to interact with character, we need to disable the group
            // TODO: We shouldn't do that, the character collision should be disabled before instead.
            CPhysicSettings.Active.SetCollision(m_CharacterGroup, false);

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
        }

        private void SimulatePhysicStep(float dt)
        {
            for (int i = 0; i != m_Group.Length; i++)
            {
                var oldVelocity = m_Group.VelocityArray[i].Value;
                var motor       = m_Group.MotorArray[i];
                var gameObject  = m_Group.GameObjects[i];
                var transform   = m_Group.Transforms[i];
                var slopeLimit = motor.CharacterController.slopeLimit;
                var entity = m_Group.Entities[i];

                var layerMask = CPhysicSettings.PhysicInteractionLayerMask;
                var wasGrounded = motor.IsGrounded(layerMask);
                var wasStableOnGround = motor.IsStableOnGround;
                var wasSliding = motor.IsSliding;
                if (!wasGrounded && !wasStableOnGround && !wasSliding)
                {
                    oldVelocity += new Vector3(0, Physics.gravity.y, 0) * dt;
                    motor.CharacterController.stepOffset = 0.5f;
                }
                else if (wasGrounded && wasStableOnGround && !wasSliding)
                {
                    motor.CharacterController.stepOffset = 0.5f;
                    
                    //if (oldVelocity.y <= 0) oldVelocity.y = -motor.CharacterController.stepOffset;
                    if (oldVelocity.y <= 0) oldVelocity.y = -1;
                }
                else 
                {
                    oldVelocity.y = -1;
                }

                if (!wasGrounded && Input.GetKey(KeyCode.LeftControl) && oldVelocity.y < -2f)
                {
                    oldVelocity.y = Mathf.Lerp(oldVelocity.y, -0.25f, dt * 4f);
                }

                var oldPos      = transform.position;
                var velocity    = oldVelocity * dt;

                CPhysicSettings.Active.SetGlobalCollision(gameObject, true);
                var previousPosition = transform.position;
                var ev = motor.MoveBy(velocity);
                CPhysicSettings.Active.SetGlobalCollision(gameObject, false);
                
                var isGrounded = motor.IsGrounded(layerMask);
                var correctVelocity = oldVelocity;

                for (var j = ev.EventsStartIndex; j < ev.EventsLength; j++)
                {
                    var hitEvent = ev.GetColliderHit(j);
                    if (OnControllerHasHitACollider(hitEvent, velocity.magnitude, oldPos, motor, transform,
                        ref correctVelocity))
                        break;
                }
                
                var newPosition      = transform.position;
                var momentum         = (newPosition - previousPosition) / dt;
                var previousMomentum = motor.Momentum;

                // This is wrong, it should be done when sliding.
                // In real life, we don't keep the height momentum when running.
                /*if (wasGrounded && !isGrounded && previousMomentum.y > 0.5f && correctVelocity.y <= 0.1f)
                {
                    isM = true;
                    
                    correctVelocity.y += previousMomentum.y;

                    motor.MoveBy(Vector3.up * (previousMomentum.y * dt));
                }*/
                
                // Slide on floor (done next frame)
                Profiler.BeginSample("Slide on floor");
                Profiler.BeginSample("Get angle");
                var slideAngle = Vector3.Angle(motor.AngleDir, Vector3.up);
                Debug.DrawRay(motor.transform.position, motor.AngleDir, Color.red, 5);
                Profiler.EndSample();
                if (slideAngle > motor.CharacterController.slopeLimit && slideAngle < 80 && isGrounded)
                {                    
                    Debug.Log("Sliding");
                    var oldY = correctVelocity.y;
                    
                    var undesiredMotion = motor.AngleDir * Vector3.Dot(correctVelocity, motor.AngleDir);
                    var desiredMotion   = correctVelocity - undesiredMotion;
                    var leftOverInertia = desiredMotion * 0.5f * dt;
                    
                    correctVelocity   = desiredMotion + leftOverInertia;
                    /*var velocityToChoose = correctVelocity;
                    if (Math.Abs(velocityToChoose.y) < 0.001)
                        velocityToChoose.y = -1f;*/
                    
                    //correctVelocity = RaycastUtilities.SlideVelocity(velocityToChoose, motor.AngleDir);
                    //correctVelocity.y = oldY;
                    correctVelocity += RaycastUtilities.SlideVelocity(new Vector3(0, Physics.gravity.y, 0) * dt * (slideAngle / 90), motor.AngleDir);
                    
                    Debug.Log((slideAngle / 90));
                    //correctVelocity.y = oldY * dt;
                    //correctVelocity.y = Mathf.Clamp(correctVelocity.y, -10, 60);
                    
                    motor.AngleDir = ProbeGround(motor, transform, motor.AngleDir, 90, 90);
                    
                    motor.IsStableOnGround = false;
                    motor.IsSliding        = true;
                    //motor.CharacterController.stepOffset = 0f;
                }
                else
                {
                    Profiler.BeginSample("Set properties");
                    motor.IsStableOnGround = isGrounded;
                    motor.IsSliding        = false;
                    Profiler.EndSample();
                }
                Profiler.EndSample();

                Profiler.BeginSample("Set AngleDir");
                if (isGrounded)
                {
                    motor.AngleDir = GetAngleDir(motor, transform);
                    correctVelocity.y = Mathf.Max(correctVelocity.y, 0);
                }
                else if (wasGrounded && !isGrounded && correctVelocity.y < 0.001f)
                {                    
                    motor.AngleDir = ProbeGround(motor, transform, motor.AngleDir,
                        slopeLimit - Mathf.Clamp(correctVelocity.ToGrid(1).magnitude, 0, 15) * 3 + 15, slopeLimit);
                }
                Profiler.EndSample();

                motor.Momentum = momentum;

                var events = motor.AllColliderHitsInFrame;
                for (int x = 0; x != events.Count; x++)
                {
                    var hitEvent = events[x];
                    // TODO Fire events
                }

                if (motor.IsGrounded(layerMask) && !wasGrounded)
                {
                 MvDelegateEvents.InvokeCharacterLand(entity);   
                }
                
                m_Group.States[i] = new CharacterControllerState(motor.IsGrounded(layerMask), motor.IsStableOnGround, motor.IsSliding, motor.AngleDir);
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
            var layerMask = CPhysicSettings.PhysicInteractionLayerMask;
            var rayLength = Physics.RaycastNonAlloc(lowPoint, Vector3.down, s_RaycastHits, controller.stepOffset, layerMask);
            Profiler.EndSample();
            
            if (previousDirection == Vector3.zero)
                previousDirection = Vector3.up;
            
            var highestAngle = 0f;
            var highestDir = previousDirection;
            var highestY = float.MinValue;
            for (int i = 0; i != rayLength; i++)
            {
                var ray = s_RaycastHits[i];
                if (ray.transform == transform)
                    continue;
                
                //ray.normal = RepairHitSurfaceNormal(ray);
                
                var angle = Vector3.Angle(previousDirection, ray.normal);
                if (angle > highestAngle)
                {
                    highestAngle = angle;
                    highestDir   = ray.normal;
                }

                if (angle > maxAngle || Vector3.Angle(Vector3.up, ray.normal) > globalMaxAngle
                    && highestY > ray.point.y)
                    continue;
                
                var velocityToAdd = ray.point - lowPoint;
                
                CPhysicSettings.Active.SetGlobalCollision(motor.gameObject, true);
                motor.MoveBy(velocityToAdd);
                CPhysicSettings.Active.SetGlobalCollision(motor.gameObject, false);

                highestY = ray.point.y;

                motor.IsGroundForcedThisFrame = true;
            }

            return highestDir;
        }
        
        private bool CheckGround(CharacterControllerMotor motor, Transform transform)
        {
            var controller = motor.CharacterController;

            var worldCenter = transform.position + controller.center;
            var lowPoint    = worldCenter - new Vector3(0, controller.height * 0.5f, 0);

            Profiler.BeginSample("RaycastNonAlloc");
            var layerMask = CPhysicSettings.PhysicInteractionLayerMask;
            var rayLength = Physics.RaycastNonAlloc(lowPoint, Vector3.down, s_RaycastHits, 0.0001f, layerMask);
            Profiler.EndSample();
            
            for (int i = 0; i != rayLength; i++)
            {
                var ray = s_RaycastHits[i];
                if (ray.transform == transform)
                    continue;
                
                var velocityToAdd = ray.point - lowPoint;
                
                CPhysicSettings.Active.SetGlobalCollision(motor.gameObject, true);
                motor.MoveBy(velocityToAdd);
                CPhysicSettings.Active.SetGlobalCollision(motor.gameObject, false);

                return true;
            }

            return false;
        }

        private Vector3 GetAngleDir(CharacterControllerMotor motor, Transform transform)
        {
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
                Physics.Raycast(ray.point + Vector3.up * 0.01f, Vector3.down, out ray, ray.distance + 0.02f);

                var normal = ray.normal;
                //var normal = RepairHitSurfaceNormal(ray);
                var angle  = Vector3.Angle(Vector3.up, normal);
                
                Debug.DrawRay(ray.point, normal, Color.green, 5);
                
                if (angle < smallestAngle)
                {
                    smallestAngle = angle;
                    smallestDir   = normal;
                }
            }

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