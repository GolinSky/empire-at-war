using UnityEngine;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>
    /// Motion state for dead reckoning: position, heading (degrees, 0 = +Z, clockwise), planar speed along the
    /// heading, turn rate and climb rate. Host prediction and clients extrapolate with the same model.
    /// </summary>
    public readonly struct EntityMotion
    {
        private const float MIN_TURN_RATE = 0.01f;

        public readonly Vector3 Position;
        public readonly float Heading;
        public readonly float Speed;
        public readonly float TurnRate;
        public readonly float ClimbRate;

        public EntityMotion(Vector3 position, float heading, float speed, float turnRate, float climbRate)
        {
            Position = position;
            Heading = heading;
            Speed = speed;
            TurnRate = turnRate;
            ClimbRate = climbRate;
        }

        /// <summary>Constant speed, turn rate and climb rate: an arc in the plane plus a vertical drift.</summary>
        public EntityMotion Extrapolate(float seconds)
        {
            float h0 = Heading * Mathf.Deg2Rad;
            float h1 = (Heading + TurnRate * seconds) * Mathf.Deg2Rad;
            Vector2 planar;
            if (Mathf.Abs(TurnRate) < MIN_TURN_RATE)
            {
                planar = new Vector2(Mathf.Sin(h0), Mathf.Cos(h0)) * (Speed * seconds);
            }
            else
            {
                float radius = Speed / (TurnRate * Mathf.Deg2Rad);
                planar = new Vector2(Mathf.Cos(h0) - Mathf.Cos(h1), Mathf.Sin(h1) - Mathf.Sin(h0)) * radius;
            }

            Vector3 position = Position + new Vector3(planar.x, ClimbRate * seconds, planar.y);
            return new EntityMotion(position, Heading + TurnRate * seconds, Speed, TurnRate, ClimbRate);
        }

        public static EntityMotion Lerp(EntityMotion from, EntityMotion to, float t) =>
            new EntityMotion(
                Vector3.Lerp(from.Position, to.Position, t),
                Mathf.LerpAngle(from.Heading, to.Heading, t),
                Mathf.Lerp(from.Speed, to.Speed, t),
                Mathf.Lerp(from.TurnRate, to.TurnRate, t),
                Mathf.Lerp(from.ClimbRate, to.ClimbRate, t));

        /// <summary>Derives rates from two pose samples taken <paramref name="deltaTime"/> apart.</summary>
        public static EntityMotion FromSamples(Vector3 previous, float previousHeading, Vector3 current, float heading,
            float deltaTime)
        {
            if (deltaTime <= 0f) return new EntityMotion(current, heading, 0f, 0f, 0f);
            Vector3 delta = current - previous;
            float speed = new Vector2(delta.x, delta.z).magnitude / deltaTime;
            float turnRate = Mathf.DeltaAngle(previousHeading, heading) / deltaTime;
            return new EntityMotion(current, heading, speed, turnRate, delta.y / deltaTime);
        }
    }
}
