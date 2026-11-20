using EmpireAtWar.Services.Camera;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Camera
{
    [Category("BattleInput")]
    public sealed class CameraPanBoostTests
    {
        private const float DELAY = 2f;
        private const float RAMP_DURATION = 4f;
        private const float MAX_MULTIPLIER = 3f;

        [Test]
        public void Step_StaysAtNormalSpeedUntilDelayPasses()
        {
            CameraPanBoost boost = new CameraPanBoost();

            Assert.That(Hold(boost, 1f), Is.EqualTo(1f));
            Assert.That(Hold(boost, 1f), Is.EqualTo(1f));
        }

        [Test]
        public void Step_SpeedsUpSlowlyThenFaster()
        {
            CameraPanBoost boost = new CameraPanBoost();
            Hold(boost, DELAY);

            float first = Hold(boost, 1f);
            float second = Hold(boost, 1f);
            float third = Hold(boost, 1f);

            Assert.That(first, Is.GreaterThan(1f));
            Assert.That(second - first, Is.GreaterThan(first - 1f));
            Assert.That(third - second, Is.GreaterThan(second - first));
        }

        [Test]
        public void Step_CapsAtMaxMultiplier()
        {
            CameraPanBoost boost = new CameraPanBoost();

            Assert.That(Hold(boost, DELAY + RAMP_DURATION * 2f), Is.EqualTo(MAX_MULTIPLIER));
        }

        [Test]
        public void Step_ReleasingPanRestartsTheDelay()
        {
            CameraPanBoost boost = new CameraPanBoost();
            Hold(boost, DELAY + RAMP_DURATION);

            Assert.That(boost.Step(false, DELAY, RAMP_DURATION, MAX_MULTIPLIER, 0.1f), Is.EqualTo(1f));
            Assert.That(Hold(boost, 1f), Is.EqualTo(1f));
        }

        [Test]
        public void Reset_RestartsTheDelay()
        {
            CameraPanBoost boost = new CameraPanBoost();
            Hold(boost, DELAY + RAMP_DURATION);

            boost.Reset();

            Assert.That(Hold(boost, 1f), Is.EqualTo(1f));
        }

        private static float Hold(CameraPanBoost boost, float seconds)
        {
            return boost.Step(true, DELAY, RAMP_DURATION, MAX_MULTIPLIER, seconds);
        }
    }
}
