using System.Collections.Generic;

namespace EmpireAtWar.Services.ShipAbilities
{
    public sealed class AutoPowerMonitor
    {
        private readonly Queue<(float Time, float Loss)> _losses = new Queue<(float, float)>();
        private float _lastTime = float.NegativeInfinity;
        private float _lastShields;
        private float _maxShields;
        private bool _suppressed;
        private int _shieldRevision;
        private AutoPowerDecision _decision;
        private int _checks;
        public float ManualUntil { get; set; }

        public bool Sample(float now, float shields, float maximum, bool suppressed, AutoPowerTuning tuning, int shieldRevision = 0)
        {
            bool reset = maximum != _maxShields || suppressed != _suppressed || shieldRevision != _shieldRevision ||
                now - _lastTime > tuning.ShieldLossWindow;
            if (reset) { _losses.Clear(); ResetConfirmation(); }
            else if (!suppressed && shields < _lastShields) _losses.Enqueue((now, _lastShields - shields));
            _lastTime = now; _lastShields = shields; _maxShields = maximum; _suppressed = suppressed;
            _shieldRevision = shieldRevision;
            while (_losses.Count > 0 && (now - _losses.Peek().Time > tuning.ShieldLossWindow || _losses.Count > 64)) _losses.Dequeue();
            float loss = 0f;
            foreach (var sample in _losses) loss += sample.Loss;
            return maximum > 0f && loss >= maximum * tuning.HeavyShieldLoss;
        }

        public bool Confirm(AutoPowerDecision decision, int required)
        {
            _checks = decision == _decision ? _checks + 1 : 1;
            _decision = decision;
            return decision != AutoPowerDecision.None && _checks >= required;
        }

        public void ResetConfirmation() { _checks = 0; _decision = AutoPowerDecision.None; }

        public void Suspend()
        {
            _losses.Clear();
            _lastTime = float.NegativeInfinity;
            ResetConfirmation();
        }
    }
}
