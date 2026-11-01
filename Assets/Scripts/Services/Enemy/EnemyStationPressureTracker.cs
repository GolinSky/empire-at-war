using System;
using System.Collections.Generic;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>Own-station damage and visible contact memory shared by every group of one player.</summary>
    public sealed class EnemyStationPressureTracker
    {
        private const float DAMAGE_LATCH_FRACTION = 0.005f;
        private const float DAMAGE_WINDOW_SECONDS = 5f;
        private const float QUIET_SECONDS = 30f;
        private const float CRITICAL_FRACTION = 0.5f;
        private readonly Queue<(float Time, float Loss)> _losses = new Queue<(float, float)>();
        private long _stationId;
        private float _durability;
        private float _maximumDurability;
        private float _observedAt;
        private float _lastPressureAt;
        private float _windowLoss;
        private float _startedAt;

        public bool HasPressure { get; private set; }
        public float DurabilityFraction => _maximumDurability > 0f ? _durability / _maximumDurability : 1f;
        public float EstimatedTimeToLossSeconds { get; private set; } = float.PositiveInfinity;
        public bool IsStationCritical => HasPressure &&
            (DurabilityFraction < CRITICAL_FRACTION || EstimatedTimeToLossSeconds < 120f);
        public float LastDamageTime { get; private set; } = float.NegativeInfinity;
        public bool HasThreatPosition { get; private set; }
        public float ThreatX { get; private set; }
        public float ThreatY { get; private set; }
        public float ThreatZ { get; private set; }

        public void ObserveThreatPosition(float x, float y, float z)
        {
            HasThreatPosition = true;
            ThreatX = x;
            ThreatY = y;
            ThreatZ = z;
        }

        public void Update(long stationId, float durability, float maximumDurability, float time, long visibleThreatId = 0)
        {
            if (_stationId != stationId)
            {
                Reset();
                _stationId = stationId;
                _durability = durability;
                _startedAt = time;
                _observedAt = time;
            }
            _maximumDurability = maximumDurability;
            float loss = Math.Max(0f, _durability - durability);
            _durability = durability;
            if (loss > 0f)
            {
                _losses.Enqueue((time, loss));
                _windowLoss += loss;
                LastDamageTime = time;
                _lastPressureAt = time;
            }
            while (_losses.Count > 0 && time - _losses.Peek().Time > DAMAGE_WINDOW_SECONDS)
                _windowLoss -= _losses.Dequeue().Loss;
            if (visibleThreatId != 0 || _windowLoss >= _maximumDurability * DAMAGE_LATCH_FRACTION && _windowLoss > 0f)
                HasPressure = true;
            if (visibleThreatId != 0) _lastPressureAt = time;
            if (time - _lastPressureAt >= QUIET_SECONDS)
            {
                HasPressure = false;
                HasThreatPosition = false;
            }
            if (time > _observedAt)
            {
                float duration = Math.Min(DAMAGE_WINDOW_SECONDS, time - _startedAt);
                EstimatedTimeToLossSeconds = _windowLoss > 0f && duration > 0f
                    ? durability * duration / _windowLoss : float.PositiveInfinity;
                _observedAt = time;
            }
        }

        public void Reset()
        {
            _stationId = 0;
            _durability = 0f;
            _maximumDurability = 0f;
            _windowLoss = 0f;
            _lastPressureAt = float.NegativeInfinity;
            _losses.Clear();
            HasPressure = false;
            HasThreatPosition = false;
            LastDamageTime = float.NegativeInfinity;
            EstimatedTimeToLossSeconds = float.PositiveInfinity;
        }
    }
}
