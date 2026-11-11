using System;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Editor.AI;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.Squadrons.Data;
using EmpireAtWar.Services.Enemy;
using NUnit.Framework;
using UnityEditor;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Balance")]
    public sealed class BomberBalanceTests
    {
        private const string WEAPONS_PATH = "Assets/Settings/Data/Models/Weapon/WeaponsData.asset";
        private const string MATRIX_PATH = "Assets/Settings/Data/Models/Weapon/DamageMatrixData.asset";

        [TestCase("ARC170", 36f, 12f, 175f)]
        [TestCase("XWing", 40f, 10f, 230f)]
        public void FighterTorpedoes_MatchLiveLoadoutAndDamageTargets(string name, float damage, float reload, float dps)
        {
            SquadronData data = AssetDatabase.LoadAssetAtPath<SquadronData>(
                $"Assets/Settings/Data/Squadron/{name}SquadronData.asset");
            WeaponsData weapons = AssetDatabase.LoadAssetAtPath<WeaponsData>(WEAPONS_PATH);
            var loadout = WeaponLoadoutBaker.DeriveLoadout(WeaponLoadoutBaker.LoadSquadronView(data));
            Assert.That(loadout, Is.EqualTo(data.WeaponLoadout));
            WeaponProfile Profile(WeaponType type) => weapons.GetProfile(type)
                .WithWarheadMultipliers(data.WarheadDamageMultiplier, data.WarheadReloadMultiplier);
            WeaponLoadoutEntry torpedoes = loadout.Single(entry => Profile(entry.WeaponType).Family == WeaponFamily.FighterTorpedo);
            WeaponProfile torpedo = Profile(torpedoes.WeaponType);

            Assert.That(torpedoes.Count, Is.EqualTo(data.MemberCount));
            Assert.That(torpedo.ShotsPerSalvo, Is.EqualTo(1));
            Assert.That(torpedo.Damage, Is.EqualTo(damage).Within(0.001f));
            Assert.That(torpedo.Reload, Is.EqualTo(reload).Within(0.001f));
            Assert.That(loadout.Sum(entry => entry.Count * Profile(entry.WeaponType).DamagePerSecond),
                Is.EqualTo(dps).Within(0.001f));
        }

        [TestCase("TIEBomber", 240f)]
        [TestCase("YWingBomber", 360f)]
        [TestCase("YWing", 300f)]
        [TestCase("NTB630", 288f)]
        [TestCase("DroidBomber", 180f)]
        [TestCase("TIEPunisher", 276f)]
        public void BomberTorpedoes_MatchFactionVolleyAndReloadTargets(string name, float expectedVolley)
        {
            SquadronData data = AssetDatabase.LoadAssetAtPath<SquadronData>(
                $"Assets/Settings/Data/Squadron/{name}SquadronData.asset");
            WeaponsData weapons = AssetDatabase.LoadAssetAtPath<WeaponsData>(WEAPONS_PATH);
            DamageMatrixData matrix = AssetDatabase.LoadAssetAtPath<DamageMatrixData>(MATRIX_PATH);
            float volley = 0f;
            float piercingDps = 0f;
            foreach (WeaponLoadoutEntry entry in data.WeaponLoadout)
            {
                WeaponProfile original = weapons.GetProfile(entry.WeaponType);
                WeaponProfile adjusted = original.WithWarheadMultipliers(
                    data.WarheadDamageMultiplier, data.WarheadReloadMultiplier);
                if (original.Family == WeaponFamily.FighterTorpedo)
                {
                    volley += entry.Count * adjusted.Damage * adjusted.ShotsPerSalvo;
                    Assert.That(adjusted.Reload, Is.EqualTo(12f));
                    Assert.That(weapons.GetProfile(entry.WeaponType).Reload, Is.EqualTo(original.Reload),
                        "Squadron tuning must not mutate shared weapon profiles.");
                }
                else if (original.Family == WeaponFamily.FighterMissile)
                {
                    Assert.That(adjusted.Reload, Is.EqualTo(original.Reload * 1.5f));
                    Assert.That(adjusted.Damage, Is.EqualTo(original.Damage * data.WarheadDamageMultiplier));
                }
                else
                    Assert.That(adjusted, Is.SameAs(original), "Warhead tuning must not change lasers or ions.");

                if (matrix.IsShieldPiercing(adjusted.DamageType))
                    piercingDps += entry.Count * adjusted.DamagePerSecond *
                        matrix.GetAccuracy(adjusted.DamageType, ShipClass.Capital) *
                        matrix.GetDamageMultiplier(adjusted.DamageType, ShipClass.Capital);
            }

            Assert.That(volley, Is.EqualTo(expectedVolley).Within(0.001f));
            UnitCombatProfile rated = UnitCombatProfileFactory.Create(data.ShipClass,
                data.MemberHull * data.MemberCount, data.MemberShields * data.MemberCount,
                data.WeaponLoadout, weapons, matrix, Array.Empty<UnitCombatProfile>(),
                data.WarheadDamageMultiplier, data.WarheadReloadMultiplier);
            Assert.That(rated.PiercingDps(ShipClass.Capital), Is.EqualTo(piercingDps).Within(0.001f));
        }
    }
}
