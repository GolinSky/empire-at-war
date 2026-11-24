using UnityEngine;

namespace EmpireAtWar.Components.Ship.EngineGlow
{
    /// <summary>
    /// Packs an engine glow fade into one renderer user value for EngineGlow.shader, and mirrors the
    /// shader's fade so a new state can start from the brightness currently on screen.
    /// Bits 0-7: 255 - start level * 255. Bits 8-9: target state. Bits 10-31: fade start in 10 ms ticks.
    /// </summary>
    public static class EngineGlowUserValue
    {
        public const uint SETTLED_THRUST = 0u;
        public const float TICKS_PER_SECOND = 100f;
        public const uint TIME_MASK = 0x3FFFFFu;

        private const uint FROM_MASK = 0xFFu;
        private const int STATE_SHIFT = 8;
        private const uint STATE_MASK = 0x3u;
        private const int TIME_SHIFT = 10;
        private const float MIN_FADE_SECONDS = 0.001f;

        public static uint Encode(float fromLevel, EngineGlowState target, float startSeconds)
        {
            uint from = FROM_MASK - (uint)Mathf.RoundToInt(Mathf.Clamp01(fromLevel) * FROM_MASK);
            return from | ((uint)target << STATE_SHIFT) | (ToTicks(startSeconds) << TIME_SHIFT);
        }

        public static float Evaluate(uint value, float nowSeconds, float idleIntensity, float fadeSeconds)
        {
            float fromLevel = 1f - (value & FROM_MASK) / (float)FROM_MASK;
            float targetLevel = GetLevel((EngineGlowState)((value >> STATE_SHIFT) & STATE_MASK), idleIntensity);
            uint elapsedTicks = (ToTicks(nowSeconds) - (value >> TIME_SHIFT)) & TIME_MASK;
            float progress = Mathf.Clamp01(elapsedTicks / (TICKS_PER_SECOND * Mathf.Max(fadeSeconds, MIN_FADE_SECONDS)));
            return Mathf.Lerp(fromLevel, targetLevel, progress);
        }

        public static float GetLevel(EngineGlowState state, float idleIntensity) => state switch
        {
            EngineGlowState.Thrust => 1f,
            EngineGlowState.Idle => idleIntensity,
            _ => 0f
        };

        private static uint ToTicks(float seconds) => (uint)(seconds * TICKS_PER_SECOND) & TIME_MASK;
    }
}
