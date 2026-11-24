using EmpireAtWar.Components.Ship.EngineGlow;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Ships")]
    public sealed class EngineGlowUserValueTests
    {
        private const float IDLE = 0.3f;
        private const float FADE = 0.5f;
        // One step of the 8-bit start level plus one 10 ms tick of fade progress.
        private const float TOLERANCE = 1f / 255f + 0.01f / FADE;

        [Test]
        public void Zero_IsFullThrustAtAnyTime()
        {
            foreach (float now in new[] { 0f, 3.7f, 12345.6f })
                Assert.That(EngineGlowUserValue.Evaluate(EngineGlowUserValue.SETTLED_THRUST, now, IDLE, FADE), Is.EqualTo(1f));
        }

        [TestCase(EngineGlowState.Thrust)]
        [TestCase(EngineGlowState.Idle)]
        [TestCase(EngineGlowState.Off)]
        public void Fade_StartsAtFromLevelAndSettlesOnTarget(EngineGlowState target)
        {
            const float start = 10f;
            const float from = 0.6f;
            uint value = EngineGlowUserValue.Encode(from, target, start);
            float expected = EngineGlowUserValue.GetLevel(target, IDLE);

            Assert.That(EngineGlowUserValue.Evaluate(value, start, IDLE, FADE), Is.EqualTo(from).Within(TOLERANCE));
            Assert.That(EngineGlowUserValue.Evaluate(value, start + FADE / 2f, IDLE, FADE),
                Is.EqualTo((from + expected) / 2f).Within(TOLERANCE));
            Assert.That(EngineGlowUserValue.Evaluate(value, start + FADE * 4f, IDLE, FADE), Is.EqualTo(expected).Within(TOLERANCE));
        }

        [Test]
        public void Levels_KeepThrustAboveIdleAboveOff()
        {
            Assert.That(EngineGlowUserValue.GetLevel(EngineGlowState.Thrust, IDLE), Is.EqualTo(1f));
            Assert.That(EngineGlowUserValue.GetLevel(EngineGlowState.Idle, IDLE), Is.EqualTo(IDLE));
            Assert.That(EngineGlowUserValue.GetLevel(EngineGlowState.Off, IDLE), Is.Zero);
        }

        [Test]
        public void Reversal_MidFade_ContinuesFromTheLevelOnScreen()
        {
            const float start = 20f;
            uint toIdle = EngineGlowUserValue.Encode(1f, EngineGlowState.Idle, start);
            float reverseAt = start + FADE * 0.4f;
            float onScreen = EngineGlowUserValue.Evaluate(toIdle, reverseAt, IDLE, FADE);

            uint toThrust = EngineGlowUserValue.Encode(onScreen, EngineGlowState.Thrust, reverseAt);

            Assert.That(onScreen, Is.LessThan(1f).And.GreaterThan(IDLE));
            Assert.That(EngineGlowUserValue.Evaluate(toThrust, reverseAt, IDLE, FADE), Is.EqualTo(onScreen).Within(TOLERANCE));
            Assert.That(EngineGlowUserValue.Evaluate(toThrust, reverseAt + FADE, IDLE, FADE), Is.EqualTo(1f).Within(TOLERANCE));
        }

        [Test]
        public void Fade_AcrossTheTickWrap_KeepsElapsedTime()
        {
            float wrapSeconds = (EngineGlowUserValue.TIME_MASK + 1u) / EngineGlowUserValue.TICKS_PER_SECOND;
            float start = wrapSeconds - FADE / 4f;
            uint value = EngineGlowUserValue.Encode(1f, EngineGlowState.Off, start);

            float afterWrap = EngineGlowUserValue.Evaluate(value, start + FADE / 2f, IDLE, FADE);

            // Large times lose float precision: allow one more tick.
            Assert.That(afterWrap, Is.EqualTo(0.5f).Within(TOLERANCE * 2f));
        }
    }
}
