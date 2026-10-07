using System;

namespace EmpireAtWar.Entities.EnemyFaction.Models.Intel
{
    /// <summary>
    /// Fuzzy trust in a sighting by its age. Two fuzzy sets, Fresh and Stale, fire two rules:
    /// IF fresh THEN trust fully; IF stale THEN trust half. Confidence is the strongest rule activation,
    /// so old reports fade smoothly instead of vanishing, and are forgotten once neither set applies.
    /// </summary>
    public static class IntelConfidence
    {
        /// <summary>Seconds a sighting stays fully fresh.</summary>
        public const float FRESH_AGE = 20f;

        /// <summary>Age at which a sighting is fully stale (no longer fresh).</summary>
        public const float STALE_AGE = 90f;

        /// <summary>Age at which a sighting is forgotten.</summary>
        public const float FORGET_AGE = 240f;

        private const float STALE_TRUST = 0.5f;

        public static float Evaluate(float age)
        {
            float fresh = age <= FRESH_AGE ? 1f : Ramp(STALE_AGE, FRESH_AGE, age);
            float stale = age <= STALE_AGE ? Ramp(FRESH_AGE, STALE_AGE, age) : Ramp(FORGET_AGE, STALE_AGE, age);
            return Math.Max(fresh, STALE_TRUST * stale);
        }

        /// <summary>0 at <paramref name="zero"/>, 1 at <paramref name="one"/>, clamped; works in either direction.</summary>
        private static float Ramp(float zero, float one, float value) =>
            Math.Min(1f, Math.Max(0f, (value - zero) / (one - zero)));
    }
}
