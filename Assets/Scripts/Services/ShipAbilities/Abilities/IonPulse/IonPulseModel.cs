using System;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    public sealed class IonPulseModel
    {
        private readonly float _chargeDuration;
        private readonly float _alignmentTimeout;
        private readonly float _speed;
        private readonly float _range;
        private float _elapsed;

        public bool IsCharged => _elapsed >= _chargeDuration;
        public bool AlignmentExpired => _elapsed >= _chargeDuration + _alignmentTimeout;
        public bool HasFired { get; private set; }
        public float PreviousDistance { get; private set; }
        public float Distance { get; private set; }
        public bool WaveComplete => HasFired && Distance >= _range;

        public IonPulseModel(float chargeDuration, float alignmentTimeout, float speed, float range)
        {
            _chargeDuration = chargeDuration;
            _alignmentTimeout = alignmentTimeout;
            _speed = speed;
            _range = range;
        }

        public void Advance(float deltaTime)
        {
            _elapsed += deltaTime;
            if (!HasFired) return;
            PreviousDistance = Distance;
            Distance = Math.Min(_range, Distance + _speed * deltaTime);
        }

        public void Fire() => HasFired = true;

        public bool Intersects(float forwardDistance, float radialDistance, float radius, float thickness) =>
            HasFired && forwardDistance >= PreviousDistance - thickness &&
            forwardDistance <= Distance + thickness && radialDistance <= radius;
    }
}
