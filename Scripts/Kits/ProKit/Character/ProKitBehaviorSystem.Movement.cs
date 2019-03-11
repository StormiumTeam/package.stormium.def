using System.Collections.Generic;
using StormiumTeam.GameBase;
using StandardAssets.Characters.Physics;
using Stormium.Default.States;
using StormiumShared.Core;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

namespace package.stormium.def.Kits.ProKit
{
    public partial class ProKitBehaviorSystem
    {
        enum MovementType
        {
            None,
            Dodge,
            RayTrace,
            ApplyVelocity
        }
        
        private struct CollisionInfo
        {
            public Vector3 StartingPoint;
            public OpenCharacterController.CollisionInfo Data;
            public MovementType State;
        }
        
        private List<CollisionInfo> m_Collisions;
        private MovementType m_State;
        private OpenCharacterController m_CurrentController;
        
        private void SimulateMovements()
        {
            // Simulate movements
            ForEach((Entity                  entity,
                     OpenCharacterController controller,
                     ref ProKitMovementSettings  movementSettings,
                     ref ProKitMovementState movementState,
                     ref Velocity            velocity,
                     ref ProKitInputState    inputState,
                     ref HealthState health) =>
            {
                if (health.Health <= 0)
                    return;

                var time = GetSingleton<GameTimeComponent>().Value;
                var transform         = controller.transform;
                var position          = transform.position;
                var rotation          = transform.rotation;
                var direction         = SrtMovement.ComputeDirection(rotation, inputState.Movement);
                var directionFwd      = SrtMovement.ComputeDirectionFwd(transform.forward, rotation, inputState.Movement);
                var invDirectionFwd      = SrtMovement.ComputeDirectionFwd(transform.forward, rotation, new float2(-inputState.Movement.x, inputState.Movement.y));
                if (!controller.isGrounded) // maybe we are grounded, but because we jumped and we go against a stair, the controller think we are not grounded
                {
                    // this is a bit buggy at the moment
                    /*var oldPos = controller.transform.position;
                    controller.Move(Vector3.down * 0.00011f);
                    controller.SetPosition(oldPos, false);*/
                } 
                
                m_QueryManager.EnableCollisionFor(entity);
                    
                var startedFromGround = controller.isGrounded && movementState.ForceUnground == 0;

                movementState.ForceUnground = 0;

                m_State = MovementType.None;
                m_CurrentController = controller;

                // Listen to collisions information
                AttachToCollisionEvent(controller);

                // Fix the velocity in case.
                velocity.Value = SrtMovement.SrtFixNaN(velocity.Value);

                var groundTime = math.max(-movementState.AirTime, 0);
                var canJump      = startedFromGround && !controller.startedSlide && inputState.QueueJump >= 1; // todo: queue jump instead
                var canDodge     = startedFromGround && !controller.startedSlide && inputState.QueueDodge >= 1 && !canJump && groundTime > 0.1f;
                var canGroundRun = startedFromGround && !canJump && !canDodge;
                var canAerialRun = !startedFromGround;
                var canWallBounce = canAerialRun && (inputState.QueueJump == 2 || inputState.QueueDodge == 2) && movementState.WallBounceTick + 100 < Tick;
                
                // -------------------------------------------------------- //
                // Update gravity.
                if (startedFromGround)
                {
                    velocity.Value.y = 0;

                    if (!canJump)
                    {
                        movementState.AirTime = math.min(movementState.AirTime - TickDelta, 0);
                    }

                    movementState.AirControl = 1.0f;
                }
                else
                {
                    velocity.Value.y     -= 18f * time.DeltaTime;
                    velocity.Value.x = Mathf.Lerp(velocity.Value.x, 0.0f, time.DeltaTime * 0.05f);
                    velocity.Value.z = Mathf.Lerp(velocity.Value.z, 0.0f, time.DeltaTime * 0.05f);
                    
                    movementState.AirTime = math.max(movementState.AirTime + TickDelta, 0);
                }
                
                // -------------------------------------------------------- //
                // Ground Run function.
                if (canGroundRun)
                {
                    velocity.Value = SrtMovement.GroundMove(velocity.Value, inputState.Movement, direction, movementSettings.GroundSettings, time.DeltaTime);
                }

                // -------------------------------------------------------- //
                // Aerial run function.
                if (canAerialRun)
                {
                    var control     = math.clamp(1 - math.clamp(movementState.AirTime * 0.0005f, 0, 1), 0.25f, 1);
                    var airSettings = movementSettings.AerialSettings;
                    airSettings.Control *= control * movementState.AirControl;

                    velocity.Value = SrtMovement.AerialMove(velocity.Value, direction, airSettings, time.DeltaTime);
                }

                // -------------------------------------------------------- //
                // Jump function.
                if (canJump)
                {
                    var strafeAngle = SrtMovement.GetStrafeAngleNormalized(direction, math.float3(velocity.Value.x, 0, velocity.Value.z));
                    velocity.Value += direction * (strafeAngle * 0.5f);

                    var verticalPower = 8f;
                    // We are currently chaining jumps
                    if (movementState.AirTime - TickDelta >= 100)
                        verticalPower *= 0.75f;
                        
                    // TODO: Queue a new jump (For now, we don't queue it, we do it now)
                    velocity.Value.y = verticalPower;
                }

                // -------------------------------------------------------- //
                // Dodge function.
                if (canDodge)
                {
                    m_State = MovementType.Dodge;
                    
                    velocity.Value = SrtMovement.GroundDodge(velocity.Value, directionFwd, 0.5f, 15f, 16.5f);

                    controller.Move(math.normalizesafe(velocity.Value) * 0.1f);

                    velocity.Value.y = 4f;

                    movementState.AirTime = 0;
                }

                if (canWallBounce)
                {
                    m_State = MovementType.RayTrace;

                    var collisionFlags = controller.Move(directionFwd * 0.6f) | controller.Move(invDirectionFwd * 0.6f);
                    if ((collisionFlags & CollisionFlags.Sides) != 0)
                    {
                        Vector3 normal = default;

                        foreach (var c in m_Collisions)
                        {
                            if (c.State != MovementType.RayTrace)
                                continue;
                            if (math.abs(c.Data.normal.y) > 0.1f)
                                continue;
                            
                            normal = c.Data.normal;
                        }

                        if (normal != default && velocity.Value.y >= 2f)
                        {
                            var bounce = normal * 6f;
                            bounce.y += 6f;

                            velocity.Value =  RayUtility.SlideVelocityNoYChange(velocity.Value, normal);
                            velocity.Value += (float3) (bounce);

                            movementState.WallBounceTick = Tick;
                            movementState.AirControl *= 0.5f;
                        }
                        else if (normal != default && velocity.Value.y < 2f && movementState.AirTime >= 125)
                        {
                            var oldY = velocity.Value.y;
                            var reflected = Vector3.Reflect(directionFwd, normal);
                            var slideNormal = RayUtility.SlideVelocityNoYChange(velocity.Value, normal);
                            var dirInertia = (RayUtility.SlideVelocityNoYChange(velocity.Value, normal) * 1f) + normal * 5f;

                            velocity.Value   = dirInertia + reflected * 1.5f + slideNormal;
                            velocity.Value.y = math.max(oldY + 3, 3);

                            movementState.WallBounceTick =  Tick;
                            movementState.AirControl     *= 0.1f;
                        }
                    }

                    controller.SetPosition(position, false);
                }
                
                m_State = MovementType.ApplyVelocity;

                var avPos = transform.position;
                var avY = velocity.Value.y;
                
                Profiler.BeginSample("Move()");
                var finalFlags = controller.Move(velocity.Value * time.DeltaTime);
                if ((finalFlags & CollisionFlags.Above) != 0)
                {
                    avY = math.min(velocity.Value.y, 0.0f);
                }

                if ((finalFlags & CollisionFlags.Sides) != 0)
                {
                    velocity.Value = Vector3.Lerp(velocity.Value, (transform.position - avPos) / time.DeltaTime, 0.5f);
                }

                Profiler.EndSample();
                velocity.Value.y = avY;
                
                foreach (var c in m_Collisions)
                {
                    if (c.State != MovementType.ApplyVelocity)
                        continue;

                    /*if (c.moveDirection.y < 0)
                    {
                        continue;
                    }

                    var angle        = Vector3.Angle(c.normal, Vector3.down);
                    var flatVelocity = new float3(velocity.Value.x, 0, velocity.Value.z);
                    var flatNormal   = new float3(c.normal.x, 0, c.normal.z);

                    var undesiredMotion = flatNormal * Vector3.Dot(flatVelocity, flatNormal);
                    var desiredMotion   = flatVelocity - undesiredMotion;
                    var desiredY        = desiredMotion.y;

                    desiredMotion.y = 0;

                    desiredMotion = Vector3.ClampMagnitude(desiredMotion, math.length(flatVelocity));

                    desiredMotion.y = velocity.Value.y;
                    velocity.Value  = desiredMotion;

                    // Floor
                    if ((controller.collisionFlags == CollisionFlags.Above
                         || (int) controller.collisionFlags == 3)
                        && angle < 90f && velocity.Value.y > 0)
                    {
                        Debug.Log("hello");
                        velocity.Value.y = desiredY;
                    }

                    break;*/
                }

                // Stop listening to collisions information
                DetachFromCollisionEvent(controller);
                
                m_QueryManager.ReenableCollisions();

            }, m_CharacterMovementGroup);
        }

        private void AddCollision(OpenCharacterController.CollisionInfo c)
        {
            var initialPosition = m_CurrentController.transform.position;
            
            m_Collisions.Add(new CollisionInfo{Data = c, StartingPoint = initialPosition, State = m_State});
        }

        private void AttachToCollisionEvent(OpenCharacterController characterController)
        {
            m_Collisions.Clear();
            characterController.collision += AddCollision;
        }

        private void DetachFromCollisionEvent(OpenCharacterController characterController)
        {
            characterController.collision -= AddCollision;
        }
    }
}