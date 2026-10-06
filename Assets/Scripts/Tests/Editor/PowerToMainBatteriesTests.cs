using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.ShipAbilities.Abilities;
using EmpireAtWar.ViewComponents.Health;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
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
            Assert.That(modifiers.GetMainBatteryFireDelayMultiplier(true), Is.EqualTo(0.33f));
            Assert.That(modifiers.SpeedMultiplier, Is.EqualTo(0.375f));
            Assert.That(modifiers.ShieldRegenMultiplier, Is.Zero);
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
            try
            {
                WeaponHardPoint weapon = obj.AddComponent<WeaponHardPoint>();
                CombatModifiers modifiers = new CombatModifiers();
                weapon.SetData(new WeaponProfile(), 100f, 0f, null, null, modifiers, null, null);
                MethodInfo start = typeof(WeaponHardPoint).GetMethod("TryStartScheduledSequence", PRIVATE_INSTANCE);
                MethodInfo emitting = typeof(WeaponHardPoint).GetMethod("IsEmitting", PRIVATE_INSTANCE);
                object[] args = { 0 };
                Assert.That(start.Invoke(weapon, args), Is.True);
                Assert.That(emitting.Invoke(weapon, new[] { args[0] }), Is.True);
                modifiers.SetMainBatteries(true, 0.33f);
                Assert.That(emitting.Invoke(weapon, new[] { args[0] }), Is.False);
                typeof(WeaponHardPoint).GetMethod("StopEmitting", PRIVATE_INSTANCE).Invoke(weapon, new[] { args[0] });
                Assert.That(start.Invoke(weapon, args), Is.False);
                modifiers.SetMainBatteries(false, 1f);
                Assert.That(start.Invoke(weapon, args), Is.True);
            }
            finally { Object.DestroyImmediate(obj); }
        }

        [Test]
        public void MainBattery_BoostsShotIntervalAndRemainsBlockedByIonDisable()
        {
            GameObject obj = new GameObject("MainBattery");
            try
            {
                WeaponHardPoint weapon = obj.AddComponent<WeaponHardPoint>();
                typeof(WeaponHardPoint).GetField("mainBattery", PRIVATE_INSTANCE).SetValue(weapon, true);
                WeaponProfile profile = new WeaponProfile();
                typeof(WeaponProfile).GetField("shotInterval", PRIVATE_INSTANCE).SetValue(profile, 1f);
                CombatModifiers modifiers = new CombatModifiers();
                weapon.SetData(profile, 100f, 0f, null, null, modifiers, null, null);
                modifiers.SetMainBatteries(true, 0.33f);
                Assert.That(typeof(WeaponHardPoint).GetProperty("DelayBetweenShots", PRIVATE_INSTANCE).GetValue(weapon), Is.EqualTo(0.33f));
                MethodInfo start = typeof(WeaponHardPoint).GetMethod("TryStartScheduledSequence", PRIVATE_INSTANCE);
                object[] args = { 0 };
                modifiers.SetIonDisabled(true);
                Assert.That(start.Invoke(weapon, args), Is.False);
                modifiers.SetIonDisabled(false);
                Assert.That(start.Invoke(weapon, args), Is.True);
            }
            finally { Object.DestroyImmediate(obj); }
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
