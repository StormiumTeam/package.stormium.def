using package.stormiumteam.shared;
using Unity.Mathematics;
using UnityEngine;

namespace package.stormium.def
{
    public static class SrtMovement
    {
        /// <summary>
        /// Compute the direction from a rotation and from a given direction
        /// </summary>
        /// <param name="worldRotation">The character rotation</param>
        /// <param name="inputDirection">The direction input</param>
        /// <returns>The new direction</returns>
        public static float3 ComputeDirection(Quaternion worldRotation, float2 inputDirection)
        {
            return math.normalizesafe(worldRotation * new Vector3(inputDirection.x, 0, inputDirection.y));
        }

        public static float3 ComputeDirectionFwd(Vector3 forward, Quaternion rotation, float2 input)
        {
            var inputDirection3D = math.normalizesafe(rotation * new Vector3(input.x, 0, input.y));

            return math.normalizesafe(Vector3.Lerp(forward, inputDirection3D, 1 - (forward.magnitude - math.length(inputDirection3D))));
        }

        /// <summary>
        /// Move the character with the Aerial Srt algorithm.
        /// </summary>
        /// <param name="initialVelocity">The initial velocity to use</param>
        /// <param name="direction">The movement direction</param>
        /// <param name="settings">The movement settings</param>
        /// <param name="dt">Delta time</param>
        /// <returns>Return the new position</returns>
        public static float3 AerialMove(float3 initialVelocity, float3 direction, SrtAerialSettings settings, float dt)
        {
            // Fix NaN errors
            direction = SrtFixNaN(direction);

            var prevY = initialVelocity.y;

            var wishSpeed    = math.length(direction) * settings.BaseSpeed;
            var gridVelocity = math.float3(initialVelocity.x, 0, initialVelocity.z);
            var velocity     = initialVelocity;

            velocity = AerialAccelerate(velocity, direction, settings.Acceleration, settings.Control, dt);
            var finalVelocity      = ClampSpeed(math.float3(velocity.x, 0, velocity.z), gridVelocity, settings.BaseSpeed);
            var addSpeedFromHeight = math.clamp(-initialVelocity.y * (settings.AccelerationByHighsForce), 0, 1);

            var result = math.normalizesafe(finalVelocity) * (math.length(finalVelocity) + addSpeedFromHeight);
            result.y = prevY;
            return result;
        }

        /// <summary>
        /// Move the character with the Srt (CPMA based) algorithm.
        /// </summary>
        /// <param name="initialVelocity">The initial velocity to use</param>
        /// <param name="direction">The movement direction</param>
        /// <param name="settings">The movement settings</param>
        /// <param name="dt">Delta time</param>
        /// <returns>Return the new position</returns>
        public static float3 GroundMove(float3 initialVelocity, float2 input, float3 direction, SrtGroundSettings settings, float dt)
        {
            // Fix NaN errors
            direction = SrtFixNaN(direction);

            // Set Y axe to zero
            var prevY = initialVelocity.y;
            initialVelocity.y = 0;

            var previousSpeed = math.length(initialVelocity);
            var friction = GetFrictionPower
            (
                previousSpeed,
                settings.FrictionSpeedMin, settings.FrictionSpeedMax,
                settings.FrictionMin, settings.FrictionMax
            );

            var velocity = ApplyFriction(initialVelocity, direction, friction, settings.SurfaceFriction, settings.Acceleration,
                settings.Deacceleration, dt);
            var wishSpeed                         = math.length(direction) * settings.BaseSpeed;
            if (float.IsNaN(wishSpeed)) wishSpeed = 0;

            var strafeAngleNormalized = GetStrafeAngleNormalized(direction, math.normalize(initialVelocity));

            if (wishSpeed > settings.BaseSpeed && wishSpeed < previousSpeed)
            {
                wishSpeed = math.lerp(previousSpeed, wishSpeed, settings.DecayBaseSpeedFriction * dt);
            }

            if (input.y > 0.5f)
            {
                if (previousSpeed >= settings.BaseSpeed - 0.25f)
                {
                    wishSpeed = settings.SprintSpeed;

                    settings.Acceleration = 6f;
                }
            }

            velocity = GroundAccelerate
                (velocity, direction, wishSpeed, settings.Acceleration, math.min(strafeAngleNormalized, 0.25f), settings.DecayBaseSpeedFriction, dt);

            var nextSpeed = math.length(math.float3(velocity.x, 0, velocity.z));
            if (previousSpeed > nextSpeed && nextSpeed < settings.BaseSpeed
                                          && strafeAngleNormalized > 0.1f && strafeAngleNormalized < 0.9f)
            {
                velocity.y = 0;

                velocity = Vector3.Normalize(velocity) * math.lerp(nextSpeed, previousSpeed, math.max(1 - strafeAngleNormalized, 0.8f));
            }

            velocity.y = prevY;

            return velocity;
        }

        public static float3 SrtFixNaN(float3 original)
        {
            for (int i = 0; i != 3; i++)
            {
                if (float.IsNaN(original[i])) original[i] = 0f;
            }

            return original;
        }

        /// <summary>
        /// Get the power of the friction from the player speed
        /// </summary>
        /// <param name="speed">The player speed</param>
        /// <param name="frictionSpeedMin">The minimal speed for friction to be start</param>
        /// <param name="frictionSpeedMax">The maximal speed for friction to be stop</param>
        /// <param name="frictionMin">The minimal friction (between 0 and 1)</param>
        /// <param name="frictionMax">The maximal friction (between 0 and 1)</param>
        /// <returns>Return the new friction power</returns>
        public static float GetFrictionPower(float speed, float frictionSpeedMin, float frictionSpeedMax, float frictionMin, float frictionMax)
        {
            return Mathf.Clamp
            (
                frictionSpeedMin / Mathf.Clamp(speed, frictionSpeedMin, frictionSpeedMax),
                frictionMin, frictionMax
            );
        }

        public static float GetStrafeAngleNormalized(Vector3 direction, Vector3 velocityDirection)
        {
            return math.max(math.clamp(Vector3.Angle(direction, velocityDirection), 1, 90) / 90f, 0f);
        }

        /// <summary>
        /// Apply the friction to a given velocity
        /// </summary>
        /// <param name="velocity">The player velocity</param>
        /// <param name="direction">The direction of the player</param>
        /// <param name="friction">The friction power to use</param>
        /// <param name="groundFriction">The friction power of the surface</param>
        /// <param name="accel">The acceleration of the player</param>
        /// <param name="deaccel">The deaceleration of the player</param>
        /// <param name="dt">The delta time</param>
        /// <returns>Return a new velocity from the friction</returns>
        public static float3 ApplyFriction(float3 velocity, float3 direction, float friction, float groundFriction, float accel, float deaccel, float dt)
        {
            direction = math.normalizesafe(direction);

            var frictioned   = Vector3.MoveTowards(velocity, Vector3.zero, dt * groundFriction * friction);
            var defrictioned = Vector3.Lerp(velocity, frictioned, (1 - math.length(direction)));

            if (defrictioned.magnitude > 12)
                defrictioned = Vector3.MoveTowards(defrictioned, frictioned, dt * deaccel);

            velocity = defrictioned;

            return velocity;
        }

        /// <summary>
        /// Accelerate the player from ground from a given velocity
        /// </summary>
        /// <param name="velocity">The player velocity</param>
        /// <param name="wishDirection">The wished direction</param>
        /// <param name="wishSpeed">The wished speed</param>
        /// <param name="accelPower">The acceleration power</param>
        /// <param name="strafePower">The strafe power (think of CPMA movement)</param>
        /// <param name="dt">The delta time</param>
        /// <returns>The new velocity from the acceleration</returns>
        public static float3 GroundAccelerate(float3 velocity, float3 wishDirection, float wishSpeed, float accelPower, float strafePower, float decay, float dt)
        {
            var speed = math.lerp(math.length(velocity), math.dot(velocity, wishDirection), strafePower);

            var nextSpeed = speed + (accelPower * dt);
            if (nextSpeed >= wishSpeed && speed <= wishSpeed)
                nextSpeed = wishSpeed;

            if (math.length(wishDirection) < 0.5f)
                return velocity;

            if (nextSpeed <= wishSpeed)
            {
                var c = 10f;
                if (wishSpeed >= 10)
                    c = 1.25f;

                return Vector3.MoveTowards(math.normalizesafe(velocity), wishDirection, dt * c * accelPower) * nextSpeed;
            }

            return Vector3.ClampMagnitude(velocity + wishDirection * (accelPower * dt), math.lerp(speed, wishSpeed, decay * dt));
        }

        /// <summary>
        /// Accelerate the player from air from a given velocity
        /// </summary>
        /// <param name="velocity">The player velocity</param>
        /// <param name="wishDirection">The wished direction</param>
        /// <param name="control">The control factor</param>
        /// <param name="dt">The delta time</param>
        /// <returns>The new velocity from the acceleration</returns>
        private static float3 AerialAccelerate(float3 velocity, float3 wishDirection, float acceleration, float control, float dt)
        {
            return velocity + (wishDirection * control * dt * acceleration);
        }

        private static float3 ClampSpeed(float3 velocity, float3 initialVelocity, float speed)
        {
            return Vector3.ClampMagnitude(velocity, math.max(math.length(initialVelocity), speed));
        }

        public static float3 GroundDodge(Vector3 velocity, Vector3 wishDirection, float addForce, float minSpeed, float maxSpeed)
        {
            var oldY          = velocity.y;
            var previousSpeed = velocity.ToGrid(1).magnitude;
            velocity   += wishDirection * (velocity.ToGrid(1).magnitude + addForce);
            velocity   =  Vector3.ClampMagnitude(velocity.ToGrid(1), Mathf.Min(previousSpeed + addForce, velocity.ToGrid(1).magnitude));
            velocity.y =  oldY;

            var speed = Mathf.Min(Mathf.Max(velocity.ToGrid(1).magnitude, minSpeed), maxSpeed);

            velocity   = wishDirection * speed;
            velocity.y = oldY;

            return velocity;
        }
    }
}