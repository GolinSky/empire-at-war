using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Editor.AI;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Entities.Squadrons.Data;
using EmpireAtWar.Services.Enemy;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class WeaponLoadoutBakeTests
    {
        private const string WEAPONS_DATA = "Assets/Settings/Data/Models/Weapon/WeaponsData.asset";
        private const string DAMAGE_MATRIX = "Assets/Settings/Data/Models/Weapon/DamageMatrixData.asset";
        private const string REBAKE_HINT = "Run Tools/AI/Bake Weapon Loadouts.";

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
                    data.MemberHull * data.MemberCount, data.MemberShields * data.MemberCount,
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
            string.Join(", ", loadout.Select(entry => $"{entry.WeaponType}×{entry.Count}"));
    }
}
