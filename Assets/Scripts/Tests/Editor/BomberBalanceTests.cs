using System;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
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

        [TestCase("TIEBomber", 360f)]
        [TestCase("YWingBomber", 360f)]
        [TestCase("YWing", 360f)]
        [TestCase("NTB630", 360f)]
        [TestCase("DroidBomber", 360f)]
        [TestCase("TIEPunisher", 432f)]
        public void BomberTorpedoes_HaveComparableVolleysAndLongerReload(string name, float expectedVolley)
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
