using EmpireAtWar.Services.ShipAbilities;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Abilities")]
    public sealed class AutoPowerRulesTests
    {
        [TestCase(0.2f, 0f, true, true, true, 2f, AutoPowerDecision.Weapons)]
        [TestCase(0.2f, 0f, true, true, true, 1f, AutoPowerDecision.Shields)]
        [TestCase(1f, 0.3f, true, true, true, 2f, AutoPowerDecision.Shields)]
        [TestCase(1f, 0.3f, false, true, true, 2f, AutoPowerDecision.None)]
        [TestCase(1f, 0.8f, true, true, true, 2f, AutoPowerDecision.None)]
        [TestCase(1f, 0.8f, true, false, true, 2f, AutoPowerDecision.Weapons)]
        [TestCase(1f, 0.8f, true, false, false, 2f, AutoPowerDecision.None)]
        public void PriorityAndRegeneration(float hull, float shieldFraction, bool regenerate, bool heavyLoss,
            bool attack, float advantage, AutoPowerDecision expected)
        {
            Assert.That(AutoPowerRules.Decide(hull, shieldFraction * 100f, shieldFraction, regenerate,
                heavyLoss, true, attack, advantage, 0.6f, 0.35f, 1.5f), Is.EqualTo(expected));
        }

        [Test]
        public void NoMeaningfulEnemy_BlocksWeaponsIncludingLastStand()
        {
            Assert.That(AutoPowerRules.Decide(0.1f, 0f, 0f, false, false, false, true, 10f,
                0.6f, 0.35f, 1.5f), Is.EqualTo(AutoPowerDecision.None));
        }

        [Test]
        public void SamplesSumPositiveDropsAndExpire()
        {
            AutoPowerMonitor monitor = new AutoPowerMonitor();
            AutoPowerTuning tuning = new AutoPowerTuning();
            Assert.That(monitor.Sample(0f, 100f, 100f, false, tuning), Is.False);
            Assert.That(monitor.Sample(0.5f, 85f, 100f, false, tuning), Is.False);
            Assert.That(monitor.Sample(1f, 90f, 100f, false, tuning), Is.False);
            Assert.That(monitor.Sample(1.5f, 80f, 100f, false, tuning), Is.True);
            Assert.That(monitor.Sample(5f, 60f, 100f, false, tuning), Is.False);
        }

        [Test]
        public void SuppressionAndMaximumChangesResetDamageHistory()
        {
            AutoPowerMonitor monitor = new AutoPowerMonitor();
            AutoPowerTuning tuning = new AutoPowerTuning();
            monitor.Sample(0f, 100f, 100f, false, tuning);
            Assert.That(monitor.Sample(0.5f, 0f, 100f, true, tuning), Is.False);
            Assert.That(monitor.Sample(1f, 0f, 100f, false, tuning), Is.False);
            Assert.That(monitor.Sample(1.5f, 0f, 50f, false, tuning), Is.False);
        }

        [Test]
        public void ConfirmationRequiresConsecutiveMatchingChecksAndPausePreservesManualDelay()
        {
            AutoPowerMonitor monitor = new AutoPowerMonitor { ManualUntil = 2f };
            Assert.That(monitor.Confirm(AutoPowerDecision.Shields, 2), Is.False);
            Assert.That(monitor.Confirm(AutoPowerDecision.Weapons, 2), Is.False);
            Assert.That(monitor.Confirm(AutoPowerDecision.None, 2), Is.False);
            Assert.That(monitor.Confirm(AutoPowerDecision.Weapons, 2), Is.False);
            Assert.That(monitor.Confirm(AutoPowerDecision.Weapons, 2), Is.True);
            monitor.Suspend();
            Assert.That(monitor.ManualUntil, Is.EqualTo(2f));
            Assert.That(monitor.Confirm(AutoPowerDecision.Weapons, 2), Is.False);
        }

        [Test]
        public void SuppressionBetweenSamplesIsNotDamage()
        {
            AutoPowerMonitor monitor = new AutoPowerMonitor();
            AutoPowerTuning tuning = new AutoPowerTuning();
            monitor.Sample(0f, 100f, 100f, false, tuning, 0);
            Assert.That(monitor.Sample(0.5f, 0f, 100f, false, tuning, 2), Is.False);
        }
    }
}
