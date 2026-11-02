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

        [TestCase(0.2f, 0f, true, true, true, 2f, AutoPowerRole.Weapons)]
        [TestCase(0.2f, 0f, true, true, true, 1f, AutoPowerRole.Shields)]
        [TestCase(1f, 0.3f, true, true, true, 2f, AutoPowerRole.Shields)]
        [TestCase(1f, 0.3f, false, true, true, 2f, AutoPowerRole.None)]
        [TestCase(1f, 0.8f, true, true, true, 2f, AutoPowerRole.None)]
        [TestCase(1f, 0.8f, true, false, true, 2f, AutoPowerRole.Weapons)]
        [TestCase(1f, 0.8f, true, false, false, 2f, AutoPowerRole.None)]
        public void PriorityAndRegeneration(float hull, float shieldFraction, bool regenerate, bool heavyLoss,
            bool attack, float advantage, AutoPowerRole expected)
        {
            Assert.That(_combat.ChooseRole(hull, shieldFraction * 100f, shieldFraction, regenerate,
                heavyLoss, true, attack, advantage), Is.EqualTo(expected));
        }

        [Test]
        public void NoMeaningfulEnemy_BlocksWeaponsIncludingLastStand()
        {
            Assert.That(_combat.ChooseRole(0.1f, 0f, 0f, false, false, false, true, 10f),
                Is.EqualTo(AutoPowerRole.None));
        }

        [Test]
        public void SamplesSumPositiveDropsAndExpire()
        {
            AutoPowerMonitor monitor = new AutoPowerMonitor(_catalog.AutoPower);
            Assert.That(monitor.Sample(0f, 100f, 100f, false, 0), Is.False);
            Assert.That(monitor.Sample(0.5f, 85f, 100f, false, 0), Is.False);
            Assert.That(monitor.Sample(1f, 90f, 100f, false, 0), Is.False);
            Assert.That(monitor.Sample(1.5f, 80f, 100f, false, 0), Is.True);
            Assert.That(monitor.Sample(5f, 60f, 100f, false, 0), Is.False);
        }

        [Test]
        public void ShieldRevisionResetsDamageHistory()
        {
            AutoPowerMonitor monitor = new AutoPowerMonitor(_catalog.AutoPower);
            monitor.Sample(0f, 100f, 100f, false, 0);
            Assert.That(monitor.Sample(0.5f, 85f, 100f, false, 0), Is.False);
            Assert.That(monitor.Sample(1f, 70f, 100f, false, 1), Is.False);
            Assert.That(monitor.Sample(1.5f, 60f, 100f, false, 1), Is.False);
        }

        [Test]
        public void SuppressedDropsAreNotDamage()
        {
            AutoPowerMonitor monitor = new AutoPowerMonitor(_catalog.AutoPower);
            monitor.Sample(0f, 100f, 100f, false, 0);
            Assert.That(monitor.Sample(0.5f, 0f, 100f, true, 0), Is.False);
            Assert.That(monitor.Sample(1f, 0f, 100f, false, 0), Is.False);
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
        public void SuppressionBetweenSamplesIsNotDamage()
        {
            AutoPowerMonitor monitor = new AutoPowerMonitor(_catalog.AutoPower);
            monitor.Sample(0f, 100f, 100f, false, 0);
            Assert.That(monitor.Sample(0.5f, 0f, 100f, false, 2), Is.False);
        }
    }
}
