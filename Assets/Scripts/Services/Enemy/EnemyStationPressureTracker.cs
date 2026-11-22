using System;
using System.Collections.Generic;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>Own-station damage and visible contact memory shared by every group of one player.</summary>
    public sealed class EnemyStationPressureTracker
    {
        private const float DAMAGE_LATCH_FRACTION = 0.005f;
        private const float DAMAGE_WINDOW_SECONDS = 5f;
        private const float SUSTAINED_DAMAGE_SECONDS = 3f;
        private const float QUIET_SECONDS = 30f;
        /// <summary>Critical time-to-loss while no affordable defender is known.</summary>
        private const float FALLBACK_CRITICAL_SECONDS = 120f;
        /// <summary>Critical when the station falls within this many defender arrival times (W36).</summary>
        private const float CRITICAL_ARRIVAL_FACTOR = 2f;
        private readonly Queue<(float Time, float Loss)> _losses = new Queue<(float, float)>();
        private long _stationId;
        private float _durability;
        private float _maximumDurability;
        private float _observedAt;
        private float _lastPressureAt;
        private float _windowLoss;
        private float _startedAt;

        public long StationId => _stationId;
        public bool HasPressure { get; private set; }
        public float DurabilityFraction => _maximumDurability > 0f ? _durability / _maximumDurability : 1f;
        public float EstimatedTimeToLossSeconds { get; private set; } = float.PositiveInfinity;
        /// <summary>
        /// Arrival time (queue + build + travel) of the quickest affordable defender; infinity when none is known.
        /// Set by production each evaluation.
        /// </summary>
        public float DefenderArrivalSeconds { get; set; } = float.PositiveInfinity;
        /// <summary>
        /// The station is about to fall: its time-to-loss is below twice the arrival time of the quickest affordable
        /// defender (<see cref="FALLBACK_CRITICAL_SECONDS"/> while none is known). No durability share: stripped
        /// shields stay near 0 for a whole siege and made a slow siege read as critical for 19 min (W36).
        /// </summary>
        public bool IsStationCritical => HasPressure && EstimatedTimeToLossSeconds < CriticalTimeToLossSeconds;
        public float CriticalTimeToLossSeconds => float.IsPositiveInfinity(DefenderArrivalSeconds)
            ? FALLBACK_CRITICAL_SECONDS : CRITICAL_ARRIVAL_FACTOR * DefenderArrivalSeconds;
        /// <summary>Why the station is critical, for telemetry: <c>arrival</c>, <c>fallback</c> or <c>none</c>.</summary>
        public string CriticalCause => !IsStationCritical ? "none" :
            float.IsPositiveInfinity(DefenderArrivalSeconds) ? "fallback" : "arrival";
        public float LastDamageTime { get; private set; } = float.NegativeInfinity;
        public bool HasThreatPosition { get; private set; }
        public float ThreatX { get; private set; }
        public float ThreatY { get; private set; }
        public float ThreatZ { get; private set; }

        internal bool WasDamagedRecently(long stationId, float now, float duration) =>
            _stationId == stationId && now - LastDamageTime <= duration;

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
            }
            while (_losses.Count > 0 && time - _losses.Peek().Time > DAMAGE_WINDOW_SECONDS)
                _windowLoss -= _losses.Dequeue().Loss;
            bool sustainedDamage = loss > 0f && _losses.Count >= 2 &&
                time - _losses.Peek().Time >= SUSTAINED_DAMAGE_SECONDS &&
                _windowLoss >= _maximumDurability * DAMAGE_LATCH_FRACTION;
            if (visibleThreatId != 0 || sustainedDamage)
            {
                HasPressure = true;
                _lastPressureAt = time;
            }
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
