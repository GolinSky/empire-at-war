using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Editor.AI;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Entities.Squadrons.Data;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Enemy;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Combat")]
    public sealed class WeaponLoadoutBakeTests
    {
        private const string WEAPONS_DATA = "Assets/Settings/Data/Models/Weapon/WeaponsData.asset";
        private const string DAMAGE_MATRIX = "Assets/Settings/Data/Models/Weapon/DamageMatrixData.asset";
        private const string REBAKE_HINT = "Run Tools/Empire At War/Units/AI/Bake Weapon Loadouts.";

        [Test]
        public void ShipLoadouts_MatchViewPrefabs()
        {
            foreach (ShipData data in WeaponLoadoutBaker.LoadAll<ShipData>(WeaponLoadoutBaker.SHIP_DATA_FOLDER))
            {
                Assert.That(Describe(data.WeaponLoadout),
                    Is.EqualTo(Describe(WeaponLoadoutBaker.DeriveLoadout(WeaponLoadoutBaker.LoadShipView(data)))),
                    $"{data.name}: {REBAKE_HINT}");
            }
        }

        [Test]
        public void SquadronLoadouts_MatchViewPrefabs()
        {
            foreach (SquadronData data in
                     WeaponLoadoutBaker.LoadAll<SquadronData>(WeaponLoadoutBaker.SQUADRON_DATA_FOLDER))
            {
                GameObject view = WeaponLoadoutBaker.LoadSquadronView(data);
                Assert.That(Describe(data.WeaponLoadout),
                    Is.EqualTo(Describe(WeaponLoadoutBaker.DeriveLoadout(view))), $"{data.name}: {REBAKE_HINT}");
                Assert.That(data.MemberCount, Is.EqualTo(WeaponLoadoutBaker.DeriveMemberCount(view)).And.Positive,
                    $"{data.name}: {REBAKE_HINT}");
            }
        }

        [TestCase(WeaponType.Laser, true)]
        [TestCase(WeaponType.PointDefense, false)]
        public void CombatRating_UsesSquadronShieldBonusOnlyForStrikecraftTargets(
            WeaponType weaponType, bool canHitCapital)
        {
            WeaponsData weapons = AssetDatabase.LoadAssetAtPath<WeaponsData>(WEAPONS_DATA);
            DamageMatrixData matrix = AssetDatabase.LoadAssetAtPath<DamageMatrixData>(DAMAGE_MATRIX);
            WeaponLoadoutEntry[] loadout = { new WeaponLoadoutEntry(weaponType, 1) };
            UnitCombatProfile profile = UnitCombatProfileFactory.Create(ShipClass.Corvette, 500f, 800f,
                loadout, weapons, matrix, Array.Empty<UnitCombatProfile>());
            WeaponProfile weapon = weapons.GetProfile(weaponType);
            float rawDps = weapon.Damage * weapon.ShotsPerSalvo / weapon.Reload;
            float fighterShieldDps = HitDps(ShipClass.Fighter) *
                matrix.GetSquadronShieldMultiplier(weapon.DamageType);
            float capitalShieldDps = canHitCapital
                ? HitDps(ShipClass.Capital) * matrix.GetShipShieldMultiplier(weapon.DamageType)
                : 0f;

            Assert.That(profile.ShieldDps(ShipClass.Fighter), Is.EqualTo(fighterShieldDps).Within(0.001f));
            Assert.That(profile.ShieldDps(ShipClass.Capital), Is.EqualTo(capitalShieldDps).Within(0.001f));

            float HitDps(ShipClass target) => rawDps * weapon.GetDamageBias(target) *
                Math.Min(1f, matrix.GetAccuracy(weapon.DamageType, target) * weapon.GetAccuracyBias(target));
        }

        [TestCase(ShipClass.HeavyCruiser)]
        [TestCase(ShipClass.Capital)]
        [TestCase(ShipClass.BattleCruiser)]
        [TestCase(ShipClass.Dreadnought)]
        [TestCase(ShipClass.Corvette)]
        public void CombatRating_HeavyWeaponsApplyCaliberDamageAndAccuracy(ShipClass target)
        {
            WeaponsData weapons = AssetDatabase.LoadAssetAtPath<WeaponsData>(WEAPONS_DATA);
            DamageMatrixData matrix = AssetDatabase.LoadAssetAtPath<DamageMatrixData>(DAMAGE_MATRIX);
            WeaponProfile weapon = weapons.GetProfile(WeaponType.HeavyTurboLaser);
            UnitCombatProfile profile = UnitCombatProfileFactory.Create(ShipClass.Capital, 1000f, 1000f,
                new[] { new WeaponLoadoutEntry(WeaponType.HeavyTurboLaser, 1) },
                weapons, matrix, Array.Empty<UnitCombatProfile>());
            float expectedHitDps = weapon.Damage * weapon.ShotsPerSalvo / weapon.Reload *
                Math.Min(1f, matrix.GetAccuracy(weapon.DamageType, target) * weapon.GetAccuracyBias(target)) *
                weapon.GetDamageBias(target);

            Assert.That(profile.HullDps(target), Is.EqualTo(expectedHitDps *
                matrix.GetDamageMultiplier(weapon.DamageType, target)).Within(0.001f));
            Assert.That(profile.ShieldDps(target), Is.EqualTo(expectedHitDps *
                matrix.GetShipShieldMultiplier(weapon.DamageType)).Within(0.001f));
        }

        [Test]
        public void CombatRating_AppliesTheUnitFactionDamageScale()
        {
            WeaponsData weapons = AssetDatabase.LoadAssetAtPath<WeaponsData>(WEAPONS_DATA);
            DamageMatrixData matrix = AssetDatabase.LoadAssetAtPath<DamageMatrixData>(DAMAGE_MATRIX);
            var loadout = new[] { new WeaponLoadoutEntry(WeaponType.HeavyDualIonCannon, 1) };
            UnitCombatProfile neutral = UnitCombatProfileFactory.Create(ShipClass.Capital, 1000f, 1000f, loadout,
                weapons, matrix, Array.Empty<UnitCombatProfile>());
            UnitCombatProfile rebel = UnitCombatProfileFactory.Create(ShipClass.Capital, 1000f, 1000f, loadout,
                weapons, matrix, Array.Empty<UnitCombatProfile>(), faction: FactionType.Rebellion);
            float scale = matrix.GetFactionDamageMultiplier(FactionType.Rebellion, DamageType.Ion);

            Assert.That(rebel.ShieldDps(ShipClass.Capital),
                Is.EqualTo(neutral.ShieldDps(ShipClass.Capital) * scale).Within(0.001f));
            Assert.That(rebel.HullDps(ShipClass.Capital),
                Is.EqualTo(neutral.HullDps(ShipClass.Capital) * scale).Within(0.001f));
        }

        [Test]
        public void EveryArmedUnit_GetsACombatRating()
        {
            WeaponsData weapons = AssetDatabase.LoadAssetAtPath<WeaponsData>(WEAPONS_DATA);
            DamageMatrixData matrix = AssetDatabase.LoadAssetAtPath<DamageMatrixData>(DAMAGE_MATRIX);
            foreach (ShipData data in WeaponLoadoutBaker.LoadAll<ShipData>(WeaponLoadoutBaker.SHIP_DATA_FOLDER))
            {
                AssertRated(data.name, UnitCombatProfileFactory.Create(data.ShipClass, data.Hull, data.Shields,
                    data.WeaponLoadout, weapons, matrix, Array.Empty<UnitCombatProfile>()), data.WeaponLoadout);
            }

            foreach (SquadronData data in
                     WeaponLoadoutBaker.LoadAll<SquadronData>(WeaponLoadoutBaker.SQUADRON_DATA_FOLDER))
            {
                AssertRated(data.name, UnitCombatProfileFactory.Create(data.ShipClass,
                    data.TotalHull, data.TotalShields,
                    data.WeaponLoadout, weapons, matrix, Array.Empty<UnitCombatProfile>()), data.WeaponLoadout);
            }
        }

        private static void AssertRated(string name, UnitCombatProfile profile, IReadOnlyList<WeaponLoadoutEntry> loadout)
        {
            Assert.That(profile.Hull, Is.Positive, name);
            if (loadout.Count == 0)
            {
                return;
            }

            float total = ForceComposition.Classes.Sum(target =>
                profile.HullDps(target) + profile.PiercingDps(target));
            Assert.That(total, Is.Positive, $"{name} has weapons but deals no damage to any class.");
            Assert.That(float.IsInfinity(total) || float.IsNaN(total), Is.False, name);
        }

        private static string Describe(IEnumerable<WeaponLoadoutEntry> loadout) =>
            string.Join(", ", loadout.OrderBy(entry => entry.WeaponType).Select(entry => $"{entry.WeaponType}×{entry.Count}"));
    }
}
