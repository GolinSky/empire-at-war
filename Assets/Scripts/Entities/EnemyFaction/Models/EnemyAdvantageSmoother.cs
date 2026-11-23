using System;

namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    /// <summary>
    /// Smooths the local and fleet ratings. A higher value rises with <see cref="ADVANTAGE_SMOOTHING_SECONDS"/>.
    /// A lower value counts at once, except a local drop into the band between the retreat line and
    /// <see cref="FAR_BELOW_SHARE"/> of it: that drop must last <see cref="RETREAT_CONFIRM_SECONDS"/> (W64), so one bad
    /// sample never makes a group retreat. Until then the local rating rests just above the line.
    /// </summary>
    public sealed class EnemyAdvantageSmoother
    {
        public const float ADVANTAGE_SMOOTHING_SECONDS = 4f;
        /// <summary>How long a local rating must stay below the retreat line before it counts.</summary>
        public const float RETREAT_CONFIRM_SECONDS = 1f;
        /// <summary>A local rating below this share of the retreat line counts at once: that fight is clearly lost.</summary>
        public const float FAR_BELOW_SHARE = 0.5f;
        /// <summary>Where an unconfirmed drop rests, as a multiple of the retreat line: above it, so nobody retreats yet.</summary>
        private const float PENDING_LINE_MARGIN = 1.01f;

        private bool _initialized;
        private bool _hadLocalThreat;
        private bool _hadFleetThreat;
        private float _lastTime;
        private float _localBelowSince = float.NaN;
        public float Local { get; private set; }
        public float Fleet { get; private set; }

        /// <param name="retreatLine">Local rating at or below which the group retreats; 0 takes every drop at once.</param>
        public void Sample(float local, float fleet, float now, bool hasLocalThreat = true, bool hasFleetThreat = true,
            float retreatLine = 0f)
        {
            if (_initialized && now <= _lastTime) return;

            float blend = 1f - (float)Math.Exp(-(now - _lastTime) / ADVANTAGE_SMOOTHING_SECONDS);
            bool freshContact = !hasLocalThreat || !_hadLocalThreat;
            float previousLocal = Local;
            Local = freshContact || local <= Local ? local : Local + (local - Local) * blend;
            if (!hasLocalThreat || Local > retreatLine) _localBelowSince = float.NaN;
            else if (Local >= retreatLine * FAR_BELOW_SHARE && (freshContact || previousLocal > retreatLine))
            {
                if (float.IsNaN(_localBelowSince)) _localBelowSince = now;
                if (now - _localBelowSince < RETREAT_CONFIRM_SECONDS)
                {
                    float pending = retreatLine * PENDING_LINE_MARGIN;
                    Local = freshContact ? pending : Math.Min(previousLocal, pending);
                }
            }
            Fleet = !hasFleetThreat || !_hadFleetThreat || fleet <= Fleet ? fleet : Fleet + (fleet - Fleet) * blend;
            _hadLocalThreat = hasLocalThreat;
            _hadFleetThreat = hasFleetThreat;
            _lastTime = now;
            _initialized = true;
        }
    }
}
