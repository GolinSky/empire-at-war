using System;

namespace EmpireAtWar.Services.Audio
{
    public sealed class ShipEngineAudioModel
    {
        private const float RESPONSE = 4f;
        private const float SURGE_INTERVAL = 1.5f;
        private const float ACCELERATION_THRESHOLD = 0.12f;
        private float _previousSpeed;
        private float _surgeTimeLeft;
        private bool _accelerating;

        public float Speed { get; private set; }

        public bool Advance(float normalizedSpeed, float deltaTime)
        {
            if (deltaTime <= 0f) return false;
            normalizedSpeed = Math.Clamp(normalizedSpeed, 0f, 2f);
            Speed += (normalizedSpeed - Speed) * (1f - (float)Math.Exp(-RESPONSE * deltaTime));
            _surgeTimeLeft = Math.Max(0f, _surgeTimeLeft - deltaTime);
            bool accelerating = normalizedSpeed > 0.08f &&
                (normalizedSpeed - _previousSpeed) / deltaTime > ACCELERATION_THRESHOLD;
            bool surge = accelerating && !_accelerating && _surgeTimeLeft <= 0f;
            if (surge) _surgeTimeLeft = SURGE_INTERVAL;
            _accelerating = accelerating;
            _previousSpeed = normalizedSpeed;
            return surge;
        }
    }
}
