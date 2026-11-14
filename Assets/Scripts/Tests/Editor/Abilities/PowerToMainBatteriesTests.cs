using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.ShipAbilities.Abilities;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.ViewComponents.Weapon;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Abilities")]
    public sealed class PowerToMainBatteriesTests
    {
        private const BindingFlags PRIVATE_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void Ability_RestrictsWeaponsAndRestoresOtherModifiersOnStop()
        {
            CombatModifiers modifiers = new CombatModifiers();
            CombatStatModifier other = new CombatStatModifier(2f, 0.5f, 1.5f, 2f, 0.8f);
            modifiers.Add(other);
            PowerToMainBatteriesAbility ability = new PowerToMainBatteriesAbility(new PowerToMainBatteriesSettings());
            ability.Start(new TestFacade(modifiers), null, null);

            Assert.That(modifiers.CanFireWeapon(true), Is.True);
            Assert.That(modifiers.CanFireWeapon(false), Is.False);
            Assert.That(modifiers.GetMainBatteryFireDelayMultiplier(true), Is.EqualTo(0.5f));
            Assert.That(modifiers.SpeedMultiplier, Is.EqualTo(0.375f));
            Assert.That(modifiers.ShieldRegenMultiplier, Is.EqualTo(1f));
            Assert.That(modifiers.DamageMultiplier, Is.EqualTo(2f));

            ability.Stop();
            Assert.That(modifiers.CanFireWeapon(false), Is.True);
            Assert.That(modifiers.GetMainBatteryFireDelayMultiplier(true), Is.EqualTo(1f));
            Assert.That(modifiers.SpeedMultiplier, Is.EqualTo(1.5f));
            Assert.That(modifiers.ShieldRegenMultiplier, Is.EqualTo(2f));
            Assert.That(modifiers.FireDelayMultiplier, Is.EqualTo(0.5f));
        }

        [Test]
        public void ActiveSecondarySalvo_StopsAndCannotRestartDuringAbility()
        {
            GameObject obj = new GameObject("SecondaryWeapon");
            var prewarmer = new ShotPoolPrewarmer();
            try
            {
                WeaponHardPoint weapon = obj.AddComponent<WeaponHardPoint>();
                CombatModifiers modifiers = new CombatModifiers();
                InitializeWeapon(weapon, modifiers, prewarmer);
                Assert.That(weapon.TryStartScheduledSequence(out int generation), Is.True);
                Assert.That(weapon.IsEmitting(generation), Is.True);
                modifiers.SetMainBatteries(true, 0.33f);
                Assert.That(weapon.IsEmitting(generation), Is.False);
                weapon.StopEmitting(generation);
                Assert.That(weapon.TryStartScheduledSequence(out _), Is.False);
                modifiers.SetMainBatteries(false, 1f);
                Assert.That(weapon.TryStartScheduledSequence(out _), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(obj);
                prewarmer.Clear();
            }
        }

        [Test]
        public void MainBattery_BoostsShotIntervalAndRemainsBlockedByIonDisable()
        {
            GameObject obj = new GameObject("MainBattery");
            var prewarmer = new ShotPoolPrewarmer();
            try
            {
                WeaponHardPoint weapon = obj.AddComponent<WeaponHardPoint>();
                typeof(WeaponHardPoint).GetField("mainBattery", PRIVATE_INSTANCE).SetValue(weapon, true);
                CombatModifiers modifiers = new CombatModifiers();
                InitializeWeapon(weapon, modifiers, prewarmer);
                modifiers.SetMainBatteries(true, 0.33f);
                Assert.That(weapon.DelayBetweenShots, Is.EqualTo(0.33f));
                modifiers.SetIonDisabled(true);
                Assert.That(weapon.TryStartScheduledSequence(out _), Is.False);
                modifiers.SetIonDisabled(false);
                Assert.That(weapon.TryStartScheduledSequence(out _), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(obj);
                prewarmer.Clear();
            }
        }

        private static void InitializeWeapon(WeaponHardPoint weapon, CombatModifiers modifiers, ShotPoolPrewarmer prewarmer)
        {
            var weapons = AssetDatabase.LoadAssetAtPath<WeaponsData>(
                "Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
            var profile = new WeaponProfile(WeaponType.Laser, "Test", WeaponFamily.Laser, DamageType.Laser,
                1f, 1, 1f, 1f, 100f, false, false, weapons.GetFamily(WeaponFamily.Laser).ShotPrefab,
                100f, Color.white, Vector3.one, false, false, default, default, 0f);
            weapon.SetData(profile, 100f, 0f, null, null, modifiers, null, null, null, prewarmer);
            var pool = (ShotEffectPool)typeof(WeaponHardPoint).GetMethod("GetPool", PRIVATE_INSTANCE).Invoke(weapon, null);
            pool.Prewarm(WeaponHardPoint.ExpectedShotsInFlight(profile));
        }

        private sealed class TestFacade : IShipAbilityFacade
        {
            public CombatModifiers Modifiers { get; }
            public IReadOnlyList<ShipAbilitySlot> Slots => System.Array.Empty<ShipAbilitySlot>();
            public Vector3 WorldPosition => Vector3.zero;
            public IEntity Entity => null;
            public IHealthModelObserver Health => null;
            public float RadarRange => 0f;
            public TestFacade(CombatModifiers modifiers) { Modifiers = modifiers; }
        }
    }
}
