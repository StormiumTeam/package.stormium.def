using System.Collections.Generic;
using StormiumTeam.GameBase;
using StandardAssets.Characters.Physics;
using StormiumTeam.GameBase.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
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

        private void SimulateMovements()
        {
            // Simulate movements
            Entities.With(m_CharacterMovementAuthorityGroup).ForEach((Entity                     entity,
                                                                      OpenCharacterController    controller,
                                                                      ref ProKitMovementSettings movementSettings,
                                                                      ref ProKitMovementState    movementState,
                                                                      ref Velocity               velocity,
                                                                      ref ProKitInputState       inputState,
                                                                      ref LivableHealth          health) =>
            {
                if (health.Value <= 0)
                    return;

                var time         = GetSingleton<GameTimeComponent>();
                var transform    = controller.transform;
                var position     = transform.position;
                var rotation     = transform.rotation;
                var direction    = SrtMovement.ComputeDirection(rotation, inputState.Movement);
                var directionFwd = SrtMovement.ComputeDirectionFwd(transform.forward, rotation, inputState.Movement);
                var leftFwd      = SrtMovement.ComputeDirectionFwd(transform.forward, rotation, new float2(-1, inputState.Movement.y));
                var rightFwd     = SrtMovement.ComputeDirectionFwd(transform.forward, rotation, new float2(1, inputState.Movement.y));
                
                var startedFromGround = controller.isGrounded && !movementState.ForceUnground;

                movementState.ForceUnground = false;
                
                // Fix the velocity in case.
                velocity.Value = SrtMovement.SrtFixNaN(velocity.Value);

                var groundTime          = math.max(-movementState.AirTime, 0);
                var canJump             = startedFromGround && !controller.startedSlide && inputState.QueueJump >= 1; // todo: queue jump instead
                var canChainJump = canJump && movementState.AirTime - GameTime.DeltaTick >= 100;
                var canDodge            = startedFromGround && !controller.startedSlide && inputState.QueueDodge >= 1 && !canJump && groundTime > 100;
                var canGroundRun        = startedFromGround && !canJump && !canDodge;
                var canAerialRun        = !startedFromGround;
                var canWallBounce       = canAerialRun && (inputState.QueueJump == 2 || inputState.QueueDodge == 2) && movementState.WallBounceTick + 250 < GameTime.Tick;
                var canMakeSlideActions = Mouse.current.forwardButton.isPressed;
                var canSlide            = canMakeSlideActions && math.length(velocity.Value.xz) > movementSettings.GroundSettings.BaseSpeed;
                var canCrouch           = canGroundRun && canMakeSlideActions && !canSlide;

                // -------------------------------------------------------- //
                // Update gravity.
                if (startedFromGround)
                {
                    velocity.Value.y = 0;

                    if (!canJump)
                    {
                        movementState.AirTime = math.min(movementState.AirTime - GameTime.DeltaTick, 0);
                    }

                    movementState.AirControl = 1.0f;
                }
                else
                {
                    if (movementState.LastWallDodge <= 0 || movementState.LastWallDodge + 98 < GameTime.Tick)
                    {
                        velocity.Value.y -= 18f * time.DeltaTime;
                    }
                    else
                    { 
                        velocity.Value.y = math.max(velocity.Value.y, 0);
                    }

                    velocity.Value.x =  Mathf.Lerp(velocity.Value.x, 0.0f, time.DeltaTime * 0.05f);
                    velocity.Value.z =  Mathf.Lerp(velocity.Value.z, 0.0f, time.DeltaTime * 0.05f);

                    movementState.AirTime = math.max(movementState.AirTime + GameTime.DeltaTick, 0);
                }

                // The sliding algorithm.
                // It's the same for ground slide and wall slidew
                if (movementState.IsSliding)
                {
                    // 
                    /*if (movementState.SlideNormal == float3.zero)
                    {
                        
                    }*/

                    var speed = math.length(velocity.Value.zyx);

                    velocity.Value = math.lerp(velocity.Value, math.normalizesafe(math.normalizesafe(velocity.Value.xyz) + directionFwd) * speed, time.DeltaTime * 4);
                    velocity.Value = math.lerp(velocity.Value, float3.zero, time.DeltaTime * 0.25f);
                    canGroundRun   = false;
                }
                else
                {
                    movementState.SlideNormal = float3.zero;
                }

                // -------------------------------------------------------- //
                // Ground Run function.
                if (canGroundRun)
                {
                    var settings = movementSettings.GroundSettings;
                    if (canCrouch)
                    {
                        settings.BaseSpeed   *= 0.6f;
                        settings.SprintSpeed =  settings.BaseSpeed;
                    }

                    velocity.Value = SrtMovement.GroundMove(velocity.Value, inputState.Movement, direction, settings, time.DeltaTime);
                }

                // -------------------------------------------------------- //
                // Aerial run function.
                if (canAerialRun)
                {
                    var control     = math.clamp(1 - math.clamp(movementState.AirTime * 0.0005f, 0, 1), 0.33f, 1);
                    var airSettings = movementSettings.AerialSettings;
                    airSettings.Control *= control * movementState.AirControl;

                    velocity.Value = SrtMovement.AerialMove(velocity.Value, direction, airSettings, time.DeltaTime);
                }

                // -------------------------------------------------------- //
                // Jump function.
                if (canJump)
                {
                    movementState.LastSpecialMovement = ProKitMovementState.ESpecialMovement.Jump;
                    
                    var strafeAngle = SrtMovement.GetStrafeAngleNormalized(direction, math.float3(velocity.Value.x, 0, velocity.Value.z));
                    velocity.Value += direction * (strafeAngle * 0.5f);

                    var verticalPower = 8f;
                    // We are currently chaining jumps
                    if (canChainJump)
                        verticalPower *= 0.75f;
                    verticalPower += math.max(movementState.LastMove.y * 0.25f, 0.0f);
                    
                    // TODO: Queue a new jump (For now, we don't queue it, we do it now)
                    velocity.Value.y = verticalPower;
                }

                // -------------------------------------------------------- //
                // Dodge function.
                if (canDodge)
                {
                    movementState.LastSpecialMovement = ProKitMovementState.ESpecialMovement.Dodge;

                    var prevPos        = position;
                    var prevSlopeLimit = controller.SlopeLimit;
                    var upForce        = 0.0f;
                    var normal         = default(Vector3);

                    controller.SlopeLimit = 80;
                    controller.Move(directionFwd * 1f);

                    // Check if we can do a slide jump (should be redone with new physics)
                    /*var rayMoveId = m_MoveId++;
                    foreach (var c in m_Collisions)
                    {
                        if (c.MoveId != rayMoveId)
                            continue;
                        if (c.Data.normal.y <= 0.1f)
                            continue;

                        var angle = Vector3.Angle(c.Data.normal, Vector3.up);

                        if (angle > prevSlopeLimit && angle <= 80)
                        {
                            upForce = 1 - c.Data.normal.y;
                            normal  = c.Data.normal;
                        }
                    }*/

                    controller.SlopeLimit = prevSlopeLimit;
                    transform.position    = prevPos;

                    velocity.Value.y = 0;
                    velocity.Value   = SrtMovement.GroundDodge(velocity.Value, directionFwd, 0.5f, 14f, 16.5f);
                    velocity.Value   = RayUtility.SlideVelocity(velocity.Value, normal);

                    controller.Move(math.normalizesafe(velocity.Value) * 0.25f);

                    velocity.Value.y = 4f + math.max(upForce * 15f, 0);

                    movementState.AirTime = 0;
                }

                if (canWallBounce)
                {
                    Debug.Log("wallbounce!");
                    
                    // SHOULD BE REDONE.
                    var collisionFlags = controller.Move(directionFwd * 0.3f) | controller.Move(leftFwd * 0.3f) | controller.Move(rightFwd * 0.3f);
                    if ((collisionFlags & CollisionFlags.Sides) != 0)
                    {
                        Vector3 normal = default;

                        /*var rayMoveId = m_MoveId;
                        foreach (var c in m_Collisions)
                        {
                            if (c.MoveId != rayMoveId)
                                continue;
                            if (math.abs(c.Data.normal.y) > 0.1f)
                                continue;

                            normal = c.Data.normal;
                        }*/
                        
                        var dodge = inputState.QueueDodge == 2;
                        var normalFwdAngleSin   = math.dot(directionFwd, RayUtility.SlideVelocityNoYChange(velocity.Value, normal).normalized);
                        var normalFwdAngleCosSq = 1 - normalFwdAngleSin * normalFwdAngleSin;
                        var maxAngleCosine  = math.cos(0.46f);
                        
                        Debug.Log($"sin({normalFwdAngleSin:F4})");

                        // wall jump (we don't want the player to walljump directly after a dodge)
                        if (normal != default && (movementState.LastSpecialMovement != ProKitMovementState.ESpecialMovement.Dodge || movementState.AirTime > 250))
                        {
                            var bounce = normal * 6f;
                            bounce.y += 6f;

                            var prevSpeed = velocity.speed;

                            velocity.Value =  RayUtility.SlideVelocityNoYChange(velocity.Value, normal);
                            velocity.Value += (float3) (bounce);
                            
                            movementState.WallBounceTick =  GameTime.Tick;
                            movementState.AirControl     *= 0.5f;
                            
                            movementState.LastSpecialMovement = ProKitMovementState.ESpecialMovement.WallJump;
                        }
                        // wall dodge
                        else if (normal != default && (movementState.LastSpecialMovement & ProKitMovementState.ESpecialMovement.Dodge) != 0 && (normalFwdAngleSin >= -0.1f || velocity.speed < 6))
                        {
                            var oldY        = velocity.Value.y;
                            var reflected   = Vector3.Reflect(directionFwd, normal);
                            var slideNormal = RayUtility.SlideVelocityNoYChange(velocity.Value, normal);
                            var dirInertia  = RayUtility.SlideVelocityNoYChange(directionFwd, normal);
                            var speed       = math.max(math.length(velocity.Value.xz) + 2f, 14f);
                            //var reflectWall = RayUtility.SlideVelocityNoYChange(reflected, normal).normalized * 2.5f;

                            velocity.Value   = math.normalizesafe(math.lerp(math.lerp(reflected, velocity.normalized, 0.25f), normal, 0.25f) + (float3) dirInertia) * speed;
                            velocity.Value.y = math.max(oldY + 0.5f, 2f);

                            controller.Move(velocity.normalized * (velocity.speed * 0.1f));

                            movementState.WallBounceTick =  GameTime.Tick;
                            movementState.AirControl     *= 0.1f;
                            movementState.LastWallDodge = GameTime.Tick;
                            
                            movementState.LastSpecialMovement = ProKitMovementState.ESpecialMovement.WallDodge;
                        }
                    }

                    controller.SetPosition(position, false);
                }

                var avPos = transform.position;
                var avY   = velocity.Value.y;

                Profiler.BeginSample("Move()");
                var move = velocity.Value * time.DeltaTime;

                if (canSlide)
                {
                    controller.SlopeLimit = 80;
                }
                else
                {
                    controller.SlopeLimit = 45;
                }

                var finalFlags = controller.Move(move);
                controller.CallUpdate();
                movementState.LastMove = (transform.position - avPos) / time.DeltaTime;
                if ((finalFlags & CollisionFlags.Above) != 0)
                {
                    avY = math.min(velocity.Value.y, 0.0f);
                }

                if ((finalFlags & CollisionFlags.Sides) != 0)
                {
                    velocity.Value = Vector3.Lerp(velocity.Value, movementState.LastMove, 0.5f);
                }

                Profiler.EndSample();
                velocity.Value.y = avY;
            });
        }
    }
}