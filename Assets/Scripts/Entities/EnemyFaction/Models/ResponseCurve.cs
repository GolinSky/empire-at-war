using System;

namespace EmpireAtWar.Entities.EnemyFaction.Models
{
    /// <summary>Maps a measured value to a 0..1 utility around a threshold, with a soft ramp instead of a hard cut.</summary>
    public static class ResponseCurve
    {
        /// <summary>Width of the ramp as a share of the threshold.</summary>
        private const float SOFTNESS = 0.25f;

        /// <summary>1 at or above <paramref name="threshold"/>, 0 at 75% of it or below.</summary>
        public static float AtLeast(float value, float threshold)
        {
            float start = threshold * (1f - SOFTNESS);
            return Clamp01((value - start) / (threshold - start));
        }

        /// <summary>1 at or below <paramref name="threshold"/>, 0 at 125% of it or above.</summary>
        public static float AtMost(float value, float threshold)
        {
            float end = threshold * (1f + SOFTNESS);
            return Clamp01((end - value) / (end - threshold));
        }

        public static float When(bool condition) => condition ? 1f : 0f;

        private static float Clamp01(float value) => Math.Min(1f, Math.Max(0f, value));
    }
}
