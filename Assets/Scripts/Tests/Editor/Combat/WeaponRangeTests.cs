using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Models.Selection;
using EmpireAtWar.Services.Cheats;
using EmpireAtWar.Utils;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.ViewComponents.Weapon;
using NUnit.Framework;
using UnityEngine;
using Utilities.ScriptUtils.Time;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Combat")]
    public sealed class WeaponRangeTests
    {
        private const BindingFlags PRIVATE_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void ArtilleryRange_TracksDestructionRestorationAndFleetCommand()
        {
            GameObject hull = new GameObject("ArtilleryHull");
            try
            {
                CombatModifiers modifiers = new CombatModifiers();
                ArtilleryCapability artillery = new ArtilleryCapability();
                modifiers.Changed += () => artillery.UpdateVisionMultiplier(modifiers.VisionMultiplier);
                WeaponHardPoint missile = AddMount(hull, modifiers, WeaponFamily.Missile, DamageType.ConcussionMissile,
                    700f, Vector3.back * 30f);
                WeaponHardPoint torpedo = AddMount(hull, modifiers, WeaponFamily.IonTorpedo, DamageType.Ion,
                    450f, Vector3.right * 10f);
                WeaponHardPoint gun = AddMount(hull, modifiers, WeaponFamily.Turbolaser, DamageType.Turbolaser,
                    600f, Vector3.zero);
                artillery.RegisterMount(missile, WeaponFamily.Missile, hull.transform.position);
                artillery.RegisterMount(torpedo, WeaponFamily.IonTorpedo, hull.transform.position);
                artillery.RegisterMount(gun, WeaponFamily.Turbolaser, hull.transform.position);

                Assert.That(artillery.GetArtilleryRange(ShipClass.Capital), Is.EqualTo(670f));
                int revision = artillery.ArtilleryRevision;
                ITimer reload = (ITimer)typeof(WeaponHardPoint).GetField("_reloadTimer", PRIVATE_INSTANCE).GetValue(missile);
                reload.ChangeDelay(30f).StartTimer();
                Assert.That(missile.IsBusy, Is.True);
                modifiers.SetIonDisabled(true);
                missile.UpdateData(0.5f);
                Assert.That(artillery.ArtilleryRevision, Is.EqualTo(revision));
                Assert.That(artillery.GetArtilleryRange(ShipClass.Capital), Is.EqualTo(670f));

                missile.UpdateData(0f);
                Assert.That(artillery.ArtilleryRevision, Is.GreaterThan(revision));
                Assert.That(artillery.GetArtilleryRange(ShipClass.Capital), Is.EqualTo(440f));
                Assert.That(artillery.GetArtilleryRange(ShipClass.Fighter), Is.Zero);

                modifiers.SetFleetCommand(true);
                Assert.That(artillery.GetArtilleryRange(ShipClass.Capital), Is.EqualTo(665f));
                torpedo.UpdateData(0f);
                Assert.That(artillery.GetArtilleryRange(ShipClass.Capital), Is.Zero);
                missile.UpdateData(1f);
                Assert.That(artillery.GetArtilleryRange(ShipClass.Capital), Is.EqualTo(1020f));

                artillery.Clear();
                Assert.That(artillery.GetArtilleryRange(ShipClass.Capital), Is.Zero);
                revision = artillery.ArtilleryRevision;
                missile.UpdateData(0f);
                Assert.That(artillery.ArtilleryRevision, Is.EqualTo(revision));
            }
            finally
            {
                Object.DestroyImmediate(hull);
            }
        }

        [Test]
        public void ArtilleryRange_StaysInsideSurvivingMainBatteryReach()
        {
            GameObject hull = new GameObject("ArtilleryHull");
            try
            {
                CombatModifiers modifiers = new CombatModifiers();
                ArtilleryCapability artillery = new ArtilleryCapability();
                WeaponHardPoint missile = AddMount(hull, modifiers, WeaponFamily.Missile, DamageType.ConcussionMissile,
                    700f, Vector3.forward * 10f);
                WeaponHardPoint sternMain = AddMount(hull, modifiers, WeaponFamily.Turbolaser, DamageType.Turbolaser,
                    500f, Vector3.back * 30f);
                typeof(WeaponHardPoint).GetField("mainBattery", PRIVATE_INSTANCE).SetValue(sternMain, true);
                artillery.RegisterMount(missile, WeaponFamily.Missile, hull.transform.position);
                artillery.RegisterMount(sternMain, WeaponFamily.Turbolaser, hull.transform.position);

                Assert.That(artillery.GetArtilleryRange(ShipClass.Capital), Is.EqualTo(470f));

                sternMain.UpdateData(0f);
                Assert.That(artillery.GetArtilleryRange(ShipClass.Capital), Is.EqualTo(690f));
            }
            finally
            {
                Object.DestroyImmediate(hull);
            }
        }

        [Test]
        public void CanEngageMainWeapon_RecognizesLaunchersBeyondGunRange()
        {
            GameObject hull = new GameObject("ArtilleryHull");
            try
            {
                WeaponComponent weapon = hull.AddComponent<WeaponComponent>();
                CombatModifiers modifiers = new CombatModifiers();
                WeaponHardPoint missile = AddMount(hull, modifiers, WeaponFamily.Missile, DamageType.ConcussionMissile,
                    700f, Vector3.back * 30f);
                WeaponHardPoint gun = AddMount(hull, modifiers, WeaponFamily.Turbolaser, DamageType.Turbolaser,
                    500f, Vector3.zero);
                typeof(WeaponComponent).GetField("hardPoints", PRIVATE_INSTANCE)
                    .SetValue(weapon, new List<WeaponHardPoint> { missile, gun });
                Vector3 target = Vector3.forward * 600f;

                Assert.That(weapon.CanEngageMainWeapon(ShipClass.Capital, target), Is.True);
                missile.UpdateData(0f);
                Assert.That(weapon.CanEngageMainWeapon(ShipClass.Capital, target), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(hull);
            }
        }

        [Test]
        public void Tick_DrawsAttackRangeAtEntityRootWhenWeaponComponentIsOffset()
        {
            GameObject root = new GameObject("StationRoot");
            GameObject weaponObject = new GameObject("OffsetWeapon");
            DebugRangeCircle circle = new DebugRangeCircle(new RangeDebugModel { IsEnabled = true },
                new SelectionModel { IsSelected = true }, null, "AttackRange", Color.red);
            GameObject ring = (GameObject)typeof(DebugRangeCircle).GetField("_gameObject", PRIVATE_INSTANCE)
                .GetValue(circle);
            try
            {
                root.transform.position = new Vector3(300f, 40f, -200f);
                weaponObject.transform.SetParent(root.transform, false);
                weaponObject.transform.localPosition = new Vector3(-185f, 0f, -78f);
                WeaponComponent weapon = weaponObject.AddComponent<WeaponComponent>();
                WeaponModel model = new WeaponModel(new Dictionary<(DamageType, ShipClass), float>());
                model.SetRanges(900f, new Dictionary<ShipClass, float>());
                typeof(WeaponComponent).BaseType.GetMethod("SetModel", PRIVATE_INSTANCE)
                    .Invoke(weapon, new object[] { model });
                typeof(WeaponComponent).GetField("_attackRangeCircle", PRIVATE_INSTANCE).SetValue(weapon, circle);
                typeof(WeaponComponent).GetField("_viewTransform", PRIVATE_INSTANCE)
                    .SetValue(weapon, root.transform);
                typeof(WeaponComponent).GetField("_modifiers", PRIVATE_INSTANCE)
                    .SetValue(weapon, new CombatModifiers());

                weapon.Tick();

                Assert.That(ring.transform.position, Is.EqualTo(root.transform.position));
                Assert.That(ring.GetComponent<LineRenderer>().GetPosition(0).magnitude,
                    Is.EqualTo(model.MaxWeaponRange).Within(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(ring);
                Object.DestroyImmediate(root);
            }
        }

        private static WeaponHardPoint AddMount(GameObject hull, CombatModifiers modifiers,
            WeaponFamily family, DamageType damageType, float range, Vector3 offset)
        {
            GameObject mountObject = new GameObject(family.ToString());
            mountObject.transform.SetParent(hull.transform, false);
            mountObject.transform.localPosition = offset;
            WeaponHardPoint mount = mountObject.AddComponent<WeaponHardPoint>();
            BoltShot shot = mountObject.AddComponent<BoltShot>();
            typeof(HardPoint).GetField("spawnDestroyedExplosion", PRIVATE_INSTANCE).SetValue(mount, false);
            WeaponProfile profile = new WeaponProfile(default, family.ToString(), family, damageType, 1f,
                1, 0f, 1f, range, false, false, shot, 100f, Color.white, Vector3.one,
                false, false, default, default, 0f);
            mount.SetData(profile, range, 0f, null, null, modifiers, null, null, null);
            return mount;
        }
    }
}
