using System.Reflection;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.ViewComponents.Weapon;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Utilities.ScriptUtils.Math;
using Utilities.ScriptUtils.Time;

namespace EmpireAtWar.Tests.Weapon
{
    public sealed class WeaponTurnRateTests
    {
        private const string WEAPONS = "Assets/Settings/Data/Models/Weapon/WeaponsData.asset";

        [Test]
        public void SavedWeapons_ShipWeaponsTurnAndSquadronWeaponsDoNot()
        {
            WeaponsData weapons = AssetDatabase.LoadAssetAtPath<WeaponsData>(WEAPONS);
            foreach (WeaponDefinition definition in weapons.Weapons)
            {
                WeaponProfile profile = weapons.GetProfile(definition.WeaponType);
                if (weapons.GetFamily(profile.Family).Platform == WeaponPlatform.Squadron)
                    Assert.That(profile.TurnRate, Is.Zero, definition.WeaponType.ToString());
                else
                    Assert.That(profile.TurnRate, Is.GreaterThan(0f), definition.WeaponType.ToString());
            }
        }

        [Test]
        public void SavedCalibers_HeavierGunsTurnSlower()
        {
            WeaponsData weapons = AssetDatabase.LoadAssetAtPath<WeaponsData>(WEAPONS);

            Assert.That(weapons.GetCaliber(WeaponCaliber.Medium).TurnRateMultiplier,
                Is.LessThan(weapons.GetCaliber(WeaponCaliber.Light).TurnRateMultiplier));
            Assert.That(weapons.GetCaliber(WeaponCaliber.Heavy).TurnRateMultiplier,
                Is.LessThan(weapons.GetCaliber(WeaponCaliber.Medium).TurnRateMultiplier));
        }

        [Test]
        public void TurnTowards_TakesYawOverRateAndKeepsMountAimed()
        {
            GameObject ship = new GameObject("Ship");
            try
            {
                GameObject mount = new GameObject("Mount");
                mount.transform.SetParent(ship.transform);
                WeaponHardPoint weapon = mount.AddComponent<WeaponHardPoint>();
                FloatRange arc = new FloatRange();
                SetBound(arc, "Min", -90f);
                SetBound(arc, "Max", 90f);
                weapon.SetData(arc);
                weapon.SetData(Profile(turnRate: 45f), 100f, 0f, null, null, new CombatModifiers(), null, null, null, null);

                float firstTurn = weapon.TurnTowards(new Vector3(10f, 0f, 0f));

                Assert.That(firstTurn, Is.EqualTo(2f).Within(0.001f));
                Assert.That(weapon.IsBusy, Is.True);
                Assert.That(weapon.TurnTowards(new Vector3(20f, 0f, 0f)), Is.EqualTo(0f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(ship);
            }
        }

        [Test]
        public void TurnTowards_WithoutTurnRateFiresAtOnce()
        {
            GameObject mount = new GameObject("Mount");
            try
            {
                WeaponHardPoint weapon = mount.AddComponent<WeaponHardPoint>();
                weapon.SetData(Profile(turnRate: 0f), 100f, 0f, null, null, new CombatModifiers(), null, null, null, null);

                Assert.That(weapon.TurnTowards(new Vector3(-10f, 0f, 0f)), Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(mount);
            }
        }

        [Test]
        public void RestartAttackSequence_ClearsReloadAndTurnFromPreviousLife()
        {
            GameObject mount = new GameObject("Mount");
            try
            {
                WeaponHardPoint weapon = mount.AddComponent<WeaponHardPoint>();
                FloatRange arc = new FloatRange();
                SetBound(arc, "Min", -90f);
                SetBound(arc, "Max", 90f);
                weapon.SetData(arc);
                weapon.SetData(Profile(45f), 100f, 0f, null, null, new CombatModifiers(), null, null, null, null);
                ITimer timer = (ITimer)typeof(WeaponHardPoint).GetField("_reloadTimer",
                    BindingFlags.Instance | BindingFlags.NonPublic).GetValue(weapon);
                timer.ChangeDelay(100f).StartTimer();
                weapon.TurnTowards(Vector3.right * 10f);
                weapon.ReleaseAttackSequence();

                weapon.RestartAttackSequence();

                Assert.That(timer.TimeLeft, Is.Zero);
                Assert.That(typeof(WeaponHardPoint).GetField("_turnEndTime",
                    BindingFlags.Instance | BindingFlags.NonPublic).GetValue(weapon), Is.EqualTo(float.NegativeInfinity));
                Assert.That(weapon.TurnTowards(Vector3.forward * 10f), Is.Zero);
            }
            finally { Object.DestroyImmediate(mount); }
        }

        [Test]
        public void WeaponRange_TracksFleetCommandWithoutReinitializingMount()
        {
            GameObject mount = new GameObject("Mount");
            try
            {
                WeaponHardPoint weapon = mount.AddComponent<WeaponHardPoint>();
                CombatModifiers modifiers = new CombatModifiers();
                weapon.SetData(Profile(0f), 100f, 0f, null, null, modifiers, null, null, null, null);
                Assert.That(weapon.MaxAttackDistance, Is.EqualTo(100f));
                modifiers.SetFleetCommand(true);
                Assert.That(weapon.MaxAttackDistance, Is.EqualTo(150f));
                modifiers.SetFleetCommand(false);
                Assert.That(weapon.MaxAttackDistance, Is.EqualTo(100f));
            }
            finally { Object.DestroyImmediate(mount); }
        }

        private static WeaponProfile Profile(float turnRate) =>
            new WeaponProfile(WeaponType.Laser, "Test", WeaponFamily.Laser, DamageType.Laser, 1f, 1, 0f, 1f, 100f,
                false, false, null, 100f, Color.white, Vector3.one, false, false, default, default, 1f, turnRate);

        private static void SetBound(FloatRange range, string bound, float value) =>
            typeof(Range<float>).GetProperty(bound).GetSetMethod(true).Invoke(range, new object[] { value });
    }
}
