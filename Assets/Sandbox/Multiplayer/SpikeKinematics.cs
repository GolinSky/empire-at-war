using UnityEngine;

namespace EmpireAtWar.Sandbox.Multiplayer
{
    // Planar motion state. Heading in degrees, 0 = +Z, clockwise; position is (x, z).
    public readonly struct SpikeKinematics
    {
        private const float MIN_TURN_RATE = 0.01f;

        public readonly Vector2 Position;
        public readonly float Heading;
        public readonly float Speed;
        public readonly float TurnRate;

        public SpikeKinematics(Vector2 position, float heading, float speed, float turnRate)
        {
            Position = position;
            Heading = heading;
            Speed = speed;
            TurnRate = turnRate;
        }

        // Constant speed + constant turn rate (arc) extrapolation; same model on host predictor and clients.
        public SpikeKinematics Extrapolate(float seconds)
        {
            float h0 = Heading * Mathf.Deg2Rad;
            float h1 = (Heading + TurnRate * seconds) * Mathf.Deg2Rad;
            Vector2 position;
            if (Mathf.Abs(TurnRate) < MIN_TURN_RATE)
            {
                position = Position + new Vector2(Mathf.Sin(h0), Mathf.Cos(h0)) * (Speed * seconds);
            }
            else
            {
                float radius = Speed / (TurnRate * Mathf.Deg2Rad);
                position = Position + new Vector2(Mathf.Cos(h0) - Mathf.Cos(h1), Mathf.Sin(h1) - Mathf.Sin(h0)) * radius;
            }

            return new SpikeKinematics(position, Heading + TurnRate * seconds, Speed, TurnRate);
        }

        public static SpikeKinematics Lerp(SpikeKinematics from, SpikeKinematics to, float t)
        {
            return new SpikeKinematics(
                Vector2.Lerp(from.Position, to.Position, t),
                Mathf.LerpAngle(from.Heading, to.Heading, t),
                Mathf.Lerp(from.Speed, to.Speed, t),
                Mathf.Lerp(from.TurnRate, to.TurnRate, t));
        }
    }
}
