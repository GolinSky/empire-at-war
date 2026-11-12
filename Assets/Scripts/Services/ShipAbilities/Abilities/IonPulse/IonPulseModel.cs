using System;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    public sealed class IonPulseModel
    {
        private readonly float _speed;
        private float _range;

        public bool HasFired { get; private set; }
        public float PreviousDistance { get; private set; }
        public float Distance { get; private set; }
        public bool WaveComplete => HasFired && Distance >= _range;

        public IonPulseModel(float speed)
        {
            _speed = speed;
        }

        public void Advance(float deltaTime)
        {
            if (!HasFired) return;
            PreviousDistance = Distance;
            Distance = Math.Min(_range, Distance + _speed * deltaTime);
        }

        public void Fire(float range)
        {
            _range = range;
            HasFired = true;
        }

        public bool Intersects(float forwardDistance, float radialDistance, float radius, float thickness) =>
            HasFired && forwardDistance >= PreviousDistance - thickness &&
            forwardDistance <= Distance + thickness && radialDistance <= radius;
    }
}
