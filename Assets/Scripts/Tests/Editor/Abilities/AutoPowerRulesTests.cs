using EmpireAtWar.Services.ShipAbilities;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Abilities")]
    public sealed class AutoPowerRulesTests
    {
        private ShipAbilityCatalog _catalog;
        private AutoPowerCombatSnapshot _combat;

        // Default tuning: low shields 0.6, last-stand hull 0.35, last-stand force ratio 1.5.
        [SetUp]
        public void SetUp()
        {
            _catalog = ScriptableObject.CreateInstance<ShipAbilityCatalog>();
            _combat = new AutoPowerCombatSnapshot(null, null, null, null, null, _catalog);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_catalog);

        [TestCase(0.2f, 0f, true, false, AutoPowerShieldLoss.Heavy, true, 2f, AutoPowerRole.Weapons)]
        [TestCase(0.2f, 0f, true, false, AutoPowerShieldLoss.Heavy, true, 1f, AutoPowerRole.Shields)]
        [TestCase(1f, 0.3f, true, false, AutoPowerShieldLoss.Heavy, true, 2f, AutoPowerRole.Shields)]
        [TestCase(1f, 0.3f, false, false, AutoPowerShieldLoss.Heavy, true, 2f, AutoPowerRole.None)]
        [TestCase(1f, 0.8f, true, false, AutoPowerShieldLoss.Heavy, true, 2f, AutoPowerRole.None)]
        [TestCase(1f, 0.8f, true, false, AutoPowerShieldLoss.None, true, 2f, AutoPowerRole.Weapons)]
        [TestCase(1f, 0.8f, true, false, AutoPowerShieldLoss.None, false, 2f, AutoPowerRole.None)]
        // Steady fire on drained shields can never reach the burst threshold, yet still asks for Shields.
        [TestCase(1f, 0.1f, true, false, AutoPowerShieldLoss.Light, true, 2f, AutoPowerRole.Shields)]
        [TestCase(1f, 0.3f, true, false, AutoPowerShieldLoss.None, true, 2f, AutoPowerRole.Weapons)]
        [TestCase(1f, 0.8f, true, false, AutoPowerShieldLoss.Light, true, 2f, AutoPowerRole.Weapons)]
        // Without a shield power, light fire on low shields keeps Weapons available.
        [TestCase(1f, 0.3f, false, false, AutoPowerShieldLoss.Light, true, 2f, AutoPowerRole.Weapons)]
        // An active shield boost lets Weapons stack on top, even under heavy loss.
        [TestCase(1f, 0.1f, true, true, AutoPowerShieldLoss.Heavy, true, 2f, AutoPowerRole.Weapons)]
        [TestCase(1f, 0.1f, true, true, AutoPowerShieldLoss.Light, true, 2f, AutoPowerRole.Weapons)]
        [TestCase(1f, 0.1f, true, true, AutoPowerShieldLoss.Heavy, false, 2f, AutoPowerRole.None)]
        public void PriorityAndRegeneration(float hull, float shieldFraction, bool canBoostShields,
            bool shieldsBoosted, AutoPowerShieldLoss shieldLoss, bool attack, float advantage, AutoPowerRole expected)
        {
            Assert.That(_combat.ChooseRole(hull, shieldFraction * 100f, shieldFraction, canBoostShields,
                shieldsBoosted, shieldLoss, true, attack, advantage), Is.EqualTo(expected));
        }

        // Round 5, E11: below 40 % with a hostile warship in radar range the shield boost fires at once; the
        // older rule (below 60 % with recent shield damage) still applies after confirmation.
        [TestCase(0.39f, true, AutoPowerShieldLoss.None, AutoPowerRole.Shields, true)]
        [TestCase(0.39f, false, AutoPowerShieldLoss.None, AutoPowerRole.None, false)]
        [TestCase(0.55f, false, AutoPowerShieldLoss.Light, AutoPowerRole.Shields, false)]
        [TestCase(0.65f, true, AutoPowerShieldLoss.None, AutoPowerRole.None, false)]
        public void ShieldBoost_UrgentBelowFortyWithWarshipNear(float shieldFraction, bool warshipNear,
            AutoPowerShieldLoss shieldLoss, AutoPowerRole expected, bool urgent)
        {
            Assert.That(_combat.ChooseRole(1f, shieldFraction * 100f, shieldFraction, true, false, shieldLoss,
                false, false, 1f, warshipNear), Is.EqualTo(expected));
            Assert.That(_combat.IsUrgentShieldBoost(shieldFraction, true, false, warshipNear), Is.EqualTo(urgent));
        }

        [Test]
        public void ShieldBoost_UrgentRuleComesBeforeLastStandWeapons()
        {
            Assert.That(_combat.ChooseRole(0.2f, 0f, 0f, true, false, AutoPowerShieldLoss.Heavy, true, true, 2f,
                hostileShipNearby: true), Is.EqualTo(AutoPowerRole.Shields));
            Assert.That(_combat.IsUrgentShieldBoost(0.1f, true, true, true), Is.False, "Already boosted.");
            Assert.That(_combat.IsUrgentShieldBoost(0.1f, false, false, true), Is.False, "No shield power.");
        }

        [Test]
        public void NoMeaningfulEnemy_BlocksWeaponsIncludingLastStand()
        {
            Assert.That(_combat.ChooseRole(0.1f, 0f, 0f, false, false, AutoPowerShieldLoss.None, false, true, 10f),
                Is.EqualTo(AutoPowerRole.None));
        }

        [Test]
        public void SamplesSumPositiveDropsAndExpire()
        {
            AutoPowerMonitor monitor = new AutoPowerMonitor(_catalog.AutoPower);
            Assert.That(monitor.Sample(0f, 100f, 100f, false, 0), Is.EqualTo(AutoPowerShieldLoss.None));
            Assert.That(monitor.Sample(0.5f, 85f, 100f, false, 0), Is.EqualTo(AutoPowerShieldLoss.Light));
            Assert.That(monitor.Sample(1f, 90f, 100f, false, 0), Is.EqualTo(AutoPowerShieldLoss.Light));
            Assert.That(monitor.Sample(1.5f, 80f, 100f, false, 0), Is.EqualTo(AutoPowerShieldLoss.Heavy));
            Assert.That(monitor.Sample(5f, 60f, 100f, false, 0), Is.EqualTo(AutoPowerShieldLoss.None));
        }

        [Test]
        public void ShieldRevisionResetsDamageHistory()
        {
            AutoPowerMonitor monitor = new AutoPowerMonitor(_catalog.AutoPower);
            monitor.Sample(0f, 100f, 100f, false, 0);
            Assert.That(monitor.Sample(0.5f, 85f, 100f, false, 0), Is.EqualTo(AutoPowerShieldLoss.Light));
            Assert.That(monitor.Sample(1f, 70f, 100f, false, 1), Is.EqualTo(AutoPowerShieldLoss.None));
            Assert.That(monitor.Sample(1.5f, 60f, 100f, false, 1), Is.EqualTo(AutoPowerShieldLoss.Light));
        }

        [Test]
        public void SuppressedDropsAreNotDamage()
        {
            AutoPowerMonitor monitor = new AutoPowerMonitor(_catalog.AutoPower);
            monitor.Sample(0f, 100f, 100f, false, 0);
            Assert.That(monitor.Sample(0.5f, 0f, 100f, true, 0), Is.EqualTo(AutoPowerShieldLoss.None));
            Assert.That(monitor.Sample(1f, 0f, 100f, false, 0), Is.EqualTo(AutoPowerShieldLoss.None));
        }

        [Test]
        public void ConfirmationRequiresConsecutiveMatchingChecks()
        {
            AutoPowerMonitor monitor = new AutoPowerMonitor(_catalog.AutoPower);
            Assert.That(monitor.Confirm(AutoPowerRole.Shields), Is.False);
            Assert.That(monitor.Confirm(AutoPowerRole.Weapons), Is.False);
            Assert.That(monitor.Confirm(AutoPowerRole.None), Is.False);
            Assert.That(monitor.Confirm(AutoPowerRole.Weapons), Is.False);
            Assert.That(monitor.Confirm(AutoPowerRole.Weapons), Is.True);
            monitor.Suspend();
            Assert.That(monitor.Confirm(AutoPowerRole.Weapons), Is.False);
        }

        [Test]
        public void ManualHoldResetsConfirmationAndSurvivesSuspend()
        {
            AutoPowerMonitor monitor = new AutoPowerMonitor(_catalog.AutoPower);
            monitor.Confirm(AutoPowerRole.Weapons);
            monitor.HoldManual(0f);
            Assert.That(monitor.Confirm(AutoPowerRole.Weapons), Is.False);
            monitor.Suspend();
            Assert.That(monitor.IsHeld(1.9f), Is.True);
            Assert.That(monitor.IsHeld(2f), Is.False);
        }

        [Test]
        public void StartDelayCountsFromFirstSightAndSurvivesSuspend()
        {
            AutoPowerMonitor monitor = new AutoPowerMonitor(_catalog.AutoPower);
            Assert.That(monitor.IsWarmedUp(0f, 0f), Is.False, "An unseen caster has no clock yet.");
            monitor.MarkSeen(10f);
            monitor.MarkSeen(12f);
            Assert.That(monitor.IsWarmedUp(10f, 0f), Is.True);
            Assert.That(monitor.IsWarmedUp(14.9f, 5f), Is.False);
            monitor.Suspend();
            Assert.That(monitor.IsWarmedUp(15f, 5f), Is.True);
        }

        [Test]
        public void SuppressionBetweenSamplesIsNotDamage()
        {
            AutoPowerMonitor monitor = new AutoPowerMonitor(_catalog.AutoPower);
            monitor.Sample(0f, 100f, 100f, false, 0);
            Assert.That(monitor.Sample(0.5f, 0f, 100f, false, 2), Is.EqualTo(AutoPowerShieldLoss.None));
        }
    }
}
