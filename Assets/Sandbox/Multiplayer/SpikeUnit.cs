using UnityEngine;

namespace EmpireAtWar.Sandbox.Multiplayer
{
    // Host-only stand-in for a ship/squadron: variable-dt Update movement, like the real sim.
    public sealed class SpikeUnit
    {
        private const float ARRIVE_DISTANCE = 20f;
        private const float SLOW_DISTANCE = 120f;
        private const float TURN_GAIN = 2f;

        private readonly float _maxSpeed;
        private readonly float _maxTurnRate;
        private readonly float _acceleration;

        public SpikeUnit(byte side, SpikeUnitKind kind, Vector2 position, float heading)
        {
            Side = side;
            Kind = kind;
            bool capital = kind == SpikeUnitKind.Capital;
            _maxSpeed = capital ? 8f : 40f;
            _maxTurnRate = capital ? 15f : 120f;
            _acceleration = capital ? 2f : 20f;
            Kinematics = new SpikeKinematics(position, heading, 0f, 0f);
            Target = position;
        }

        public byte Side { get; }
        public SpikeUnitKind Kind { get; }
        public SpikeKinematics Kinematics { get; private set; }
        public Vector2 Target { get; set; }
        public bool HasArrived => (Target - Kinematics.Position).sqrMagnitude < ARRIVE_DISTANCE * ARRIVE_DISTANCE;

        public void Tick(float deltaTime)
        {
            Vector2 toTarget = Target - Kinematics.Position;
            float distance = toTarget.magnitude;
            float desiredHeading = Mathf.Atan2(toTarget.x, toTarget.y) * Mathf.Rad2Deg;
            float headingError = Mathf.DeltaAngle(Kinematics.Heading, desiredHeading);

            float turnRate = HasArrived ? 0f : Mathf.Clamp(headingError * TURN_GAIN, -_maxTurnRate, _maxTurnRate);
            float alignment = Mathf.Clamp01(1f - Mathf.Abs(headingError) / 120f);
            float desiredSpeed = HasArrived ? 0f : _maxSpeed * alignment * Mathf.Clamp01(distance / SLOW_DISTANCE + 0.2f);
            float speed = Mathf.MoveTowards(Kinematics.Speed, desiredSpeed, _acceleration * deltaTime);

            SpikeKinematics withRates = new SpikeKinematics(Kinematics.Position, Kinematics.Heading, speed, turnRate);
            SpikeKinematics stepped = withRates.Extrapolate(deltaTime);
            Kinematics = new SpikeKinematics(stepped.Position, Mathf.Repeat(stepped.Heading, 360f), speed, turnRate);
        }
    }
}
