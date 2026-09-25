using System;
using System.Numerics;

namespace EmpireAtWar.Components.Squadrons.Flight
{
    /// <summary>
    /// One strikecraft that is always in motion: it turns its nose towards a steering point at a limited
    /// turn rate and rolls into the turn proportionally to its yaw rate.
    /// </summary>
    public sealed class FighterKinematics
    {
        private const float MAX_CLIMB = 0.35f;
        private const float MIN_STEER_DISTANCE_SQR = 0.0001f;
        private const float DEG_TO_RAD = MathF.PI / 180f;
        private const float RAD_TO_DEG = 180f / MathF.PI;

        public Vector3 Position { get; private set; }
        public Vector3 Forward { get; private set; }
        public float Speed { get; private set; }
        /// <summary>Roll in degrees around the forward axis; negative rolls the right wing down.</summary>
        public float Bank { get; private set; }

        public FighterKinematics(Vector3 position, Vector3 forward, float speed)
        {
            Position = position;
            Forward = Vector3.Normalize(forward);
            Speed = speed;
        }

        public void Step(Vector3 target, float desiredSpeed, IFighterFlightData data, float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            Vector3 toTarget = target - Position;
            Vector3 desired = toTarget.LengthSquared() > MIN_STEER_DISTANCE_SQR
                ? Vector3.Normalize(toTarget)
                : Forward;
            desired.Y = Math.Clamp(desired.Y, -MAX_CLIMB, MAX_CLIMB);
            desired = Vector3.Normalize(desired);

            float previousYaw = GetYaw(Forward);
            Forward = RotateTowards(Forward, desired, data.TurnRate * DEG_TO_RAD * deltaTime);
            float yawRate = DeltaAngle(previousYaw, GetYaw(Forward)) / deltaTime;

            float targetBank = -Math.Clamp(yawRate / data.TurnRate, -1f, 1f) * data.MaxBankAngle;
            Bank += (targetBank - Bank) * Math.Min(1f, data.BankResponse * deltaTime);

            float speedStep = data.Acceleration * deltaTime;
            Speed += Math.Clamp(desiredSpeed - Speed, -speedStep, speedStep);
            Position += Forward * Speed * deltaTime;
        }

        public static Vector3 RotateTowards(Vector3 from, Vector3 to, float maxRadians)
        {
            float angle = MathF.Acos(Math.Clamp(Vector3.Dot(from, to), -1f, 1f));
            if (angle <= maxRadians)
            {
                return to;
            }

            Vector3 axis = Vector3.Cross(from, to);
            axis = axis.LengthSquared() > 1e-8f ? Vector3.Normalize(axis) : Vector3.UnitY;
            return Vector3.Normalize(Vector3.Transform(from, Quaternion.CreateFromAxisAngle(axis, maxRadians)));
        }

        /// <summary>Heading in degrees, clockwise from +Z when seen from above.</summary>
        public static float GetYaw(Vector3 direction) => MathF.Atan2(direction.X, direction.Z) * RAD_TO_DEG;

        private static float DeltaAngle(float from, float to)
        {
            float delta = (to - from) % 360f;
            if (delta > 180f) delta -= 360f;
            if (delta < -180f) delta += 360f;
            return delta;
        }
    }
}
