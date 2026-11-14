using System.Reflection;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.ViewComponents.Weapon;
using NUnit.Framework;
using UnityEngine;
using Utilities.ScriptUtils.Time;

namespace EmpireAtWar.Tests.Weapon
{
    [Category("Combat")]
    public sealed class WeaponCadenceTests
    {
        private GameObject _mount;
        private WeaponHardPoint _weapon;
        private WeaponProfile _profile;
        private CombatModifiers _modifiers;
        private ITimer _reload;

        [SetUp]
        public void SetUp()
        {
            _mount = new GameObject("Cadence mount");
            _weapon = _mount.AddComponent<WeaponHardPoint>();
            _profile = Profile(3, 0.2f, 4f, 450f, 172.9f);
            _modifiers = new CombatModifiers();
            _weapon.SetData(_profile, _profile.Range, 0f, null, null, _modifiers, null, null, null, null);
            _reload = (ITimer)typeof(WeaponHardPoint).GetField("_reloadTimer",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_weapon);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_mount);

        [TestCase(0.25f)]
        [TestCase(0.6f)]
        [TestCase(2f)]
        [TestCase(3f)]
        public void GenericFireDelay_ChangesRemainingReloadButNotShotSpacing(float multiplier)
        {
            _reload.ChangeDelay(2f).StartTimer();
            _modifiers.Add(new CombatStatModifier(1f, multiplier, 1f, 1f, 1f));

            Assert.That(_weapon.RefreshTiming(), Is.True);
            Assert.That(_reload.TimeLeft, Is.EqualTo(2f * multiplier).Within(0.001f));
            Assert.That(_weapon.DelayBetweenShots, Is.EqualTo(0.2f));
            Assert.That(_weapon.PendingShotIntervalRatio, Is.EqualTo(1f));
        }

        [Test]
        public void WeaponPower_ActivationAndExpiryRetimeRemainingWaits()
        {
            _reload.ChangeDelay(2f).StartTimer();
            CombatModifierHandle boost = _modifiers.Add(new CombatStatModifier(1f, 0.1f, 0.5f, 0.5f, 1f));
            _modifiers.SetWeaponPowerShotInterval(0.1f);

            Assert.That(_weapon.RefreshTiming(), Is.True);
            Assert.That(_reload.TimeLeft, Is.EqualTo(0.2f).Within(0.001f));
            Assert.That(_weapon.DelayBetweenShots, Is.EqualTo(0.02f).Within(0.00001f));
            Assert.That(_weapon.PendingShotIntervalRatio, Is.EqualTo(0.1f));
            Assert.That(_profile.Damage, Is.EqualTo(7f));
            Assert.That(_profile.ProjectileSpeed, Is.EqualTo(172.9f));
            Assert.That(_profile.ShotsPerSalvo, Is.EqualTo(3));

            _modifiers.Remove(boost);
            _modifiers.SetWeaponPowerShotInterval(1f);
            Assert.That(_weapon.RefreshTiming(), Is.True);
            Assert.That(_reload.TimeLeft, Is.EqualTo(2f).Within(0.001f));
            Assert.That(_weapon.DelayBetweenShots, Is.EqualTo(0.2f));
            Assert.That(_weapon.PendingShotIntervalRatio, Is.EqualTo(10f));
            Assert.That(_weapon.RefreshTiming(), Is.False);
            Assert.That(_weapon.PendingShotIntervalRatio, Is.EqualTo(1f));
        }

        [Test]
        public void StackedCadenceChanges_RetimeBothChannelsAndIgnoreUnrelatedEffects()
        {
            typeof(WeaponHardPoint).GetField("mainBattery", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_weapon, true);
            _reload.ChangeDelay(2f).StartTimer();
            _modifiers.SetFullSalvo(0.4f, 0.5f);
            _modifiers.SetMainBatteries(true, 0.5f);
            _modifiers.SetWeaponPowerShotInterval(0.1f);
            Assert.That(_weapon.RefreshTiming(), Is.True);
            Assert.That(_reload.TimeLeft, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(_weapon.DelayBetweenShots, Is.EqualTo(0.005f).Within(0.00001f));

            _modifiers.SetFleetCommand(true);
            Assert.That(_weapon.RefreshTiming(), Is.False);
            Assert.That(_weapon.PendingShotIntervalRatio, Is.EqualTo(1f));
            Assert.That(_reload.TimeLeft, Is.EqualTo(0.5f).Within(0.001f));
            _modifiers.SetWeaponPowerShotInterval(1f);
            Assert.That(_weapon.RefreshTiming(), Is.True);
            Assert.That(_weapon.DelayBetweenShots, Is.EqualTo(0.05f).Within(0.00001f));
            Assert.That(_reload.TimeLeft, Is.EqualTo(0.5f).Within(0.001f));
        }

        [TestCase(3, 0.2f, 4f, 450f, 172.9f, 6, 24)]
        [TestCase(4, 0.25f, 10f, 550f, 45.5f, 12, 56)]
        [TestCase(3, 2f, 1f, 100f, 100f, 6, 12)]
        public void InFlightCap_UsesSlowerOfReloadAndBurst(int shots, float interval, float reload,
            float range, float speed, int baseline, int boosted)
        {
            WeaponProfile profile = Profile(shots, interval, reload, range, speed);
            Assert.That(WeaponHardPoint.ExpectedShotsInFlight(profile), Is.EqualTo(baseline));
            Assert.That(WeaponHardPoint.ExpectedShotsInFlight(profile, 1f, 1f), Is.EqualTo(baseline));
            Assert.That(WeaponHardPoint.ExpectedShotsInFlight(profile, 0.1f, 0.1f), Is.EqualTo(boosted));
        }

        private static WeaponProfile Profile(int shots, float interval, float reload, float range, float speed) =>
            new WeaponProfile(WeaponType.Laser, "Cadence", WeaponFamily.Laser, DamageType.Laser,
                7f, shots, interval, reload, range, false, false, null, speed, Color.white,
                Vector3.one, false, false, default, default, 0f);
    }
}
