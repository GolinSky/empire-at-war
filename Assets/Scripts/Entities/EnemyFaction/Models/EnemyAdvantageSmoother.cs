using System;

namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    public sealed class EnemyAdvantageSmoother
    {
        public const float ADVANTAGE_SMOOTHING_SECONDS = 4f;
        private bool _initialized;
        private bool _hadLocalThreat;
        private bool _hadFleetThreat;
        private float _lastTime;
        public float Local { get; private set; }
        public float Fleet { get; private set; }

        public void Sample(float local, float fleet, float now, bool hasLocalThreat = true, bool hasFleetThreat = true)
        {
            if (_initialized && now <= _lastTime) return;

            float blend = 1f - (float)Math.Exp(-(now - _lastTime) / ADVANTAGE_SMOOTHING_SECONDS);
            Local = !hasLocalThreat || !_hadLocalThreat || local <= Local ? local : Local + (local - Local) * blend;
            Fleet = !hasFleetThreat || !_hadFleetThreat || fleet <= Fleet ? fleet : Fleet + (fleet - Fleet) * blend;
            _hadLocalThreat = hasLocalThreat;
            _hadFleetThreat = hasFleetThreat;
            _lastTime = now;
            _initialized = true;
        }
    }
}
