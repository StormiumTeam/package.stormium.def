using package.stormiumteam.shared;
using package.stormium.core;
using Unity.Entities;
using UnityEngine;

namespace package.stormium.def
{
    [UpdateAfter(typeof(UpdateRigidbodySystem))]
    [AlwaysUpdateSystem]
    public class Lol : ComponentSystem
    {
        [Inject] private Group m_Group;

        private Vector3 GetSlide(Vector3 moveDir, Vector3 normal)
        {
            return moveDir - Vector3.Dot(moveDir, normal) * normal;
        }

        private bool OnControllerHasHitACollider(ControllerColliderHit    hit,
                                                 float                    plannedDistance,
                                                 Vector3                  oldPos,
                                                 CharacterControllerMotor motor,
                                                 ref Vector3              correctedVelocity)
        {
            var transform   = motor.transform;
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

        private void ProbeGround(CharacterControllerMotor motor)
        {
            var transform  = motor.transform;
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

                //PlayerVelocity += velocityToAdd;

                motor.MoveBy(velocityToAdd);

                motor.IsGroundForcedThisFrame = true;
            }
        }

        protected override void OnUpdate()
        {
            for (var i = 0; i != m_Group.Length; i++)
            {
                var wasGrounded = m_Group.Motors[i].IsGrounded();
                var oldPos      = m_Group.Motors[i].transform.position;
                var velocity    = m_Group.Velocities[i].Velocity * Time.deltaTime;

                var ev = m_Group.Motors[i].MoveBy(velocity);

                var correctVelocity = m_Group.Velocities[i].Velocity;
                /*var correctVelocity = (m_Group.Motors[i].transform.position - oldPos) / Time.deltaTime;
                correctVelocity.y = m_Group.Velocities[i].Velocity.y;*/


                for (var j = ev.EventsStartIndex; j < ev.EventsLength; j++)
                {
                    var hitEvent = ev.GetColliderHit(j);
                    if (OnControllerHasHitACollider(hitEvent, velocity.magnitude, oldPos, m_Group.Motors[i],
                        ref correctVelocity))
                        break;
                }

                if (wasGrounded && !m_Group.Motors[i].IsGrounded()
                                && correctVelocity.y >= -0.5f && correctVelocity.y <= 0
                                && correctVelocity.ToGrid(1).magnitude < 10f)
                    ProbeGround(m_Group.Motors[i]);

                // Debug.DrawRay(oldPos, correctVelocity.ToGrid(1) * Time.deltaTime, Color.red, 0.25f, false);

                var stam = m_Group.Staminas[i];
                stam.Value = Mathf.Max(stam.Value, 0);

                stam.Value += stam.GainPerSecond * Time.deltaTime;

                stam.Value = Mathf.Min(stam.Value, stam.Max);

                m_Group.Staminas[i] = stam;

                m_Group.Velocities[i] = new DefStVelocity
                {
                    Velocity = correctVelocity
                };
            }
        }

        private struct Group
        {
            public ComponentDataArray<StCharacter>          Characters;
            public ComponentDataArray<DefStMvInput>         Inputs;
            public ComponentDataArray<DefStVelocity>        Velocities;
            public ComponentDataArray<DefStMvStamina>       Staminas;
            public ComponentArray<CharacterControllerMotor> Motors;

            public readonly int Length;
        }
    }

    /// <summary>
    ///     CPMA style movement
    /// </summary>
    [UpdateAfter(typeof(STUpdateOrder.UOMovementUpdate.Loop))]
    [UpdateBefore(typeof(STUpdateOrder.UOMovementUpdate.FixMovement))]
    [UpdateAfter(typeof(DefStMvGravitySystem))]
    public class DefStMvRunSystem : ComponentSystem
    {
        [Inject] private Group m_Group;


        protected override void OnStartRunning()
        {
            /* UpdateRigidbodySystem.OnBeforeSimulateItem += OnSimulationUpdate;
             UpdateRigidbodySystem.OnAfterSimulateItem += (delta) =>
             {
                 for (int i = 0; i != m_Group.Length; i++)
                 {
                     m_Group.Motors[i].MoveBy(m_Group.Velocities[i].Velocity * delta);
                 }
             };*/
        }


        protected override void OnUpdate()
        {
            OnSimulationUpdate(Time.deltaTime);
        }

        private void OnSimulationUpdate(float delta)
        {
            for (var i = 0; i != m_Group.Length; i++)
            {
                var input          = m_Group.Inputs[i];
                var comp           = m_Group.Components[i];
                var groundSettings = m_Group.GroundSettings[i];
                var airSettings    = m_Group.AirSettings[i];
                var motor          = m_Group.Motors[i];

                var velocityData = m_Group.Velocities[i];

                var oldY = velocityData.Velocity.y;
                if (motor.IsGrounded())
                    velocityData.Velocity = GroundMove(velocityData.Velocity.ToGrid(1),
                        true, motor.transform.rotation * ((Vector3) input.RunDirection).normalized, groundSettings,
                        comp,
                        delta);
                else
                    velocityData.Velocity = AirMove(velocityData.Velocity,
                        motor.transform.rotation * ((Vector3) input.RunDirection).normalized, airSettings, comp,
                        delta);

                velocityData.Velocity.y = oldY;

                m_Group.Velocities[i] = velocityData;
            }
        }

        // CPMA
        private Vector3 GroundMove(Vector3                    velocity,
                                   bool                       isGrounded,
                                   Vector3                    direction,
                                   DefStMvGroundEnvironnement gSettings,
                                   DefStMvRun                 runData,
                                   float                      delta)
        {
            var currSpeed = velocity.ToGrid(1).magnitude;
            var friction = gSettings.SpeedFrictionMin /
                           Mathf.Clamp(currSpeed, gSettings.SpeedFrictionMin, gSettings.SpeedFrictionMax);
            friction = Mathf.Clamp(friction, gSettings.FrictionMin, gSettings.FrictionMax);

            velocity = ApplyFriction(velocity, friction * ((0.5f - (direction.magnitude * 0.5f)) + 0.5f), gSettings, runData, delta);

            var wishSpeed = direction.magnitude;
            direction.Normalize();
            wishSpeed *= gSettings.BaseSpeed;
            
            if (wishSpeed > gSettings.BaseSpeed && wishSpeed < currSpeed)
            {
                wishSpeed = Mathf.Lerp(currSpeed, wishSpeed, (StMath.Distance(wishSpeed, currSpeed) + 1f) * delta);
            }

            velocity = Accelerate(velocity, direction, wishSpeed, runData.Acceleration, 0.25f, delta);
            
            var speed = velocity.magnitude;            
            
            velocity.Normalize();
            velocity *= speed;

            return velocity;
        }

        // Custom
        private Vector3 AirMove(Vector3                 velocity,
                                Vector3                 direction,
                                DefStMvAirEnvironnement aSettings,
                                DefStMvRun              runData,
                                float                   delta)
        {
            var dynamicAcceleration = runData.AirAcceleration;
            var wishSpeed           = direction.magnitude;
            direction.Normalize();
            wishSpeed *= aSettings.BaseSpeed;

            var wishSpeed2 = wishSpeed;
            /*if (Vector3.Dot(velocity, direction) < 0)
                dynamicAcceleration = runData.Deacceleration;*/

            var floatOrginal = velocity.ToGrid(1);
            var vec          = velocity;                  // copy
            vec += direction * aSettings.Control * delta; //< 12.5 is the force direction
            var flatModified = vec.ToGrid(1);

            // If we use Y acceleration (to allow player to move a lot faster when jumping)
            var velocityByAccelerationY =
                Vector3.ClampMagnitude(vec, Mathf.Max(velocity.magnitude, aSettings.BaseSpeed));
            // Or no
            var finalVelocity =
                Vector3.ClampMagnitude(flatModified, Mathf.Max(floatOrginal.magnitude, aSettings.BaseSpeed));

            velocity = Vector3.Lerp(finalVelocity, velocityByAccelerationY, 0.25f);


            return velocity;
        }

        private Vector3 ApplyFriction(Vector3                    currentVelocity,
                                      float                      t,
                                      DefStMvGroundEnvironnement gSettings,
                                      DefStMvRun                 runData,
                                      float                      delta)
        {
            var   vec = currentVelocity; // Equivalent to: VectorCopy();
            float speed;
            float newspeed;
            float control;
            float drop;

            vec.y = 0.0f;
            speed = vec.magnitude;
            drop  = 0.0f;

            /* Only if the player is on the ground then apply friction */
            if (m_Group.Motors[0].IsGrounded())
            {
                control = speed < runData.Acceleration ? runData.Deacceleration : speed;
                drop    = control * gSettings.GroundFriction * delta * t;
            }

            newspeed = speed - drop;
            //playerFriction = newspeed;
            if (newspeed < 0)
                newspeed = 0;
            if (speed > 0)
                newspeed /= speed;

            currentVelocity.x *= newspeed;
            currentVelocity.z *= newspeed;

            return currentVelocity;
        }

        private Vector3 Accelerate(Vector3 currentVelocity,
                                   Vector3 wishdir,
                                   float   wishspeed,
                                   float   accel,
                                   float   dotPower,
                                   float   delta)
        {
            float addspeed;
            float accelspeed;
            float currentspeed;

            currentspeed = currentVelocity.magnitude;
            currentspeed = Mathf.Lerp(currentspeed, Vector3.Dot(currentVelocity, wishdir), dotPower);
            addspeed     = wishspeed - currentspeed;
            if (addspeed <= 0)
                return currentVelocity;
            accelspeed = accel * delta * wishspeed;
            if (accelspeed > addspeed)
                accelspeed = addspeed;

            currentVelocity += accelspeed * wishdir;

            return currentVelocity;
        }

        private struct Group
        {
            public ComponentDataArray<StCharacter>                Characters;
            public ComponentDataArray<DefStMvRunExecutable>       ExecuteFlags;
            public ComponentDataArray<DefStMvInput>               Inputs;
            public ComponentDataArray<DefStMvRun>                 Components;
            public ComponentDataArray<DefStMvGroundEnvironnement> GroundSettings;
            public ComponentDataArray<DefStMvAirEnvironnement>    AirSettings;
            public ComponentDataArray<DefStVelocity>              Velocities;
            public ComponentArray<CharacterControllerMotor>       Motors;

            public readonly int Length;
        }
    }
}