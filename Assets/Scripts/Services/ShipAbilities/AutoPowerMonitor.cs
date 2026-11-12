using System.Collections.Generic;

namespace EmpireAtWar.Services.ShipAbilities
{
    public sealed class AutoPowerMonitor
    {
        private readonly Queue<(float Time, float Loss)> _losses = new Queue<(float, float)>();
        private readonly AutoPowerTuning _tuning;
        private float _lastTime = float.NegativeInfinity;
        private float _lastShields;
        private int _shieldRevision;
        private AutoPowerRole _decision;
        private int _checks;
        private float _manualUntil;

        public AutoPowerMonitor(AutoPowerTuning tuning)
        {
            _tuning = tuning;
        }

        // A new shield revision (maximum, suppression or restore changed) starts a fresh damage history.
        public AutoPowerShieldLoss Sample(float now, float shields, float maximum, bool suppressed, int shieldRevision)
        {
            if (shieldRevision != _shieldRevision || now - _lastTime > _tuning.ShieldLossWindow)
            {
                _losses.Clear();
                ResetConfirmation();
            }
            else if (!suppressed && shields < _lastShields) _losses.Enqueue((now, _lastShields - shields));
            _lastTime = now;
            _lastShields = shields;
            _shieldRevision = shieldRevision;
            while (_losses.Count > 0 && (now - _losses.Peek().Time > _tuning.ShieldLossWindow || _losses.Count > 64))
                _losses.Dequeue();
            float loss = 0f;
            foreach (var sample in _losses) loss += sample.Loss;
            if (maximum <= 0f || loss <= 0f) return AutoPowerShieldLoss.None;
            return loss >= maximum * _tuning.HeavyShieldLoss ? AutoPowerShieldLoss.Heavy : AutoPowerShieldLoss.Light;
        }

        public bool Confirm(AutoPowerRole decision)
        {
            _checks = decision == _decision ? _checks + 1 : 1;
            _decision = decision;
            return decision != AutoPowerRole.None && _checks >= _tuning.ConfirmationChecks;
        }

        public void ResetConfirmation()
        {
            _checks = 0;
            _decision = AutoPowerRole.None;
        }

        public void HoldManual(float now)
        {
            _manualUntil = now + _tuning.ManualActionDelay;
            ResetConfirmation();
        }

        public bool IsHeld(float now) => now < _manualUntil;

        // Keeps the manual hold: a paused ship still waits out the delay after a manual press.
        public void Suspend()
        {
            _losses.Clear();
            _lastTime = float.NegativeInfinity;
            ResetConfirmation();
        }
    }
}
