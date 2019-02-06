using System;
using System.Collections.Generic;
using StandardAssets.Characters.Physics;
using Stormium.Core;
using Stormium.Default.States;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace package.stormium.def.Kits.ProKit
{
    public partial class ProKitBehaviorSystem
    {
        enum MovementType
        {
            None,
            Dodge,
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
                     ref ProKitBehaviorSettings  behaviorData,
                     ref Velocity            velocity,
                     ref ProKitInputState    inputState) =>
            {
                var time              = World.GetExistingManager<StGameTimeManager>().GetTimeFromSingleton();
                var transform         = controller.transform;
                var position          = transform.position;
                var rotation          = transform.rotation;
                var direction         = SrtMovement.ComputeDirection(rotation, inputState.Movement);
                var directionFwd      = SrtMovement.ComputeDirectionFwd(transform.forward, rotation, inputState.Movement);
                var startedFromGround = controller.isGrounded;

                m_State = MovementType.None;
                m_CurrentController = controller;

                // Listen to collisions information
                AttachToCollisionEvent(controller);

                // Fix the velocity in case.
                velocity.Value = SrtMovement.SrtFixNaN(velocity.Value);

                // -------------------------------------------------------- //
                // Update gravity.
                if (controller.isGrounded)
                {
                    velocity.Value.y     = 0;
                    behaviorData.AirTime = 0f;
                }
                else
                {
                    velocity.Value.y     -= 15f * time.DeltaTime;
                    behaviorData.AirTime += time.DeltaTime;
                }

                var canJump      = startedFromGround && inputState.QueueJump == 1; // todo: queue jump instead
                var canDodge     = startedFromGround && inputState.QueueDodge == 1 && !canJump;
                var canGroundRun = startedFromGround && !canJump && !canDodge;
                var canAerialRun = !startedFromGround;

                // -------------------------------------------------------- //
                // Ground Run function.
                if (canGroundRun)
                {
                    velocity.Value = SrtMovement.GroundMove(velocity.Value, inputState.Movement, direction, behaviorData.GroundSettings, time.DeltaTime);
                }

                // -------------------------------------------------------- //
                // Aerial run function.
                if (canAerialRun)
                {
                    var control     = math.clamp(1 - math.clamp(behaviorData.AirTime * 0.5f, 0, 1), 0.5f, 1);
                    var airSettings = behaviorData.AerialSettings;
                    airSettings.Control *= control;

                    velocity.Value = SrtMovement.AerialMove(velocity.Value, direction, airSettings, time.DeltaTime);
                }

                // -------------------------------------------------------- //
                // Jump function.
                if (canJump)
                {
                    var strafeAngle = SrtMovement.GetStrafeAngleNormalized(direction, math.float3(velocity.Value.x, 0, velocity.Value.z));
                    velocity.Value += direction * (strafeAngle * 3.5f);

                    // TODO: Queue a new jump (For now, we don't queue it, we do it now)
                    velocity.Value.y = 6;
                }

                // -------------------------------------------------------- //
                // Dodge function.
                if (canDodge)
                {
                    m_State = MovementType.Dodge;
                    
                    velocity.Value = SrtMovement.GroundDodge(velocity.Value, directionFwd, 0.5f, 15f, 16.5f);

                    controller.Move(math.normalizesafe(velocity.Value) * 0.25f);

                    velocity.Value.y = 3f;

                    behaviorData.AirTime = 0f;
                }

                m_State = MovementType.ApplyVelocity;

                var avPos = transform.position;
                var avY = velocity.Value.y;
                controller.Move(velocity.Value * time.DeltaTime);
                velocity.Value = (transform.position - avPos) / time.DeltaTime;
                velocity.Value.y = avY;
                
                foreach (var c in m_Collisions)
                {
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