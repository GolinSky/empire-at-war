using System;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Services.Enemy;
using NUnit.Framework;
using UnityEditor;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class EnemyDefenseStrategyTests
    {
        private EnemyDefenseOption[] _options;

        [SetUp]
        public void SetUp()
        {
            WeaponsData weapons = AssetDatabase.LoadAssetAtPath<WeaponsData>("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
            DamageMatrixData matrix = AssetDatabase.LoadAssetAtPath<DamageMatrixData>("Assets/Settings/Data/Models/Weapon/DamageMatrixData.asset");
            _options = new[]
            {
                Option(SiteFacilityType.QuadLaserSatellite, 300f, WeaponType.DualRepeatingPointDefense),
                Option(SiteFacilityType.TurbolaserSatellite, 500f, WeaponType.DualMediumTurboLaser),
                Option(SiteFacilityType.IonSatellite, 400f, WeaponType.MediumIonCannon),
                Option(SiteFacilityType.MassDriverSatellite, 1600f, WeaponType.MassDriver),
                Option(SiteFacilityType.MissileSatellite, 1000f, WeaponType.HeavyAssaultMissile)
            };
            EnemyDefenseOption Option(SiteFacilityType type, float price, WeaponType weapon) => new EnemyDefenseOption(type, price,
                UnitCombatProfileFactory.Create(ShipClass.Structure, 1000f, 1000f,
                    new[] { new WeaponLoadoutEntry(weapon, 6) }, weapons, matrix, Array.Empty<UnitCombatProfile>()),
                weapons.GetProfile(weapon).Range);
        }

        [TestCase(ShipClass.Fighter, 600f, 0f, 800f, 0.8f, 0f, SiteFacilityType.QuadLaserSatellite)]
        [TestCase(ShipClass.Frigate, 3000f, 1000f, 1800f, 0.6f, 0f, SiteFacilityType.TurbolaserSatellite)]
        [TestCase(ShipClass.Cruiser, 5000f, 15000f, 2000f, 0.6f, 0f, SiteFacilityType.IonSatellite)]
        [TestCase(ShipClass.Capital, 10000f, 30000f, 12000f, 0.1f, 2f, SiteFacilityType.MassDriverSatellite)]
        [TestCase(ShipClass.Cruiser, 10000f, 30000f, 7000f, 0.9f, 0f, SiteFacilityType.MissileSatellite)]
        public void KnownComposition_SelectsAppropriateDefense(ShipClass target, float hull, float shields,
            float money, float exposure, float baseThreat, SiteFacilityType expected)
        {
            var hostile = Force(target, hull, shields);
            var strategy = new EnemyDefenseStrategy();
            var snapshot = Snapshot(hostile, money, 0f, Pad(exposure: exposure), baseThreat);
            if (expected == SiteFacilityType.IonSatellite)
            {
                var hullDps = new float[ForceComposition.ClassSlots];
                hullDps[(int)target] = 300f;
                snapshot.Own.AddNew(new UnitCombatProfile(ShipClass.Cruiser, 1000f, 1000f, hullDps,
                    new float[ForceComposition.ClassSlots], new float[ForceComposition.ClassSlots], Array.Empty<UnitCombatProfile>()), false);
            }
            Assert.That(strategy.TrySelect(snapshot, out var choice), Is.True);
            Assert.That(choice.Type, Is.EqualTo(expected), string.Join("; ", Describe(strategy)));
        }

        [Test]
        public void NoIntel_SelectsGeneralist()
        {
            var strategy = new EnemyDefenseStrategy();
            Assert.That(strategy.TrySelect(Snapshot(new ForceComposition(), 2000f, 0f, Pad()), out var choice), Is.True);
            Assert.That(choice.Type, Is.EqualTo(SiteFacilityType.TurbolaserSatellite));
        }

        [Test]
        public void ProductionReserve_BlocksPeacetimeSpending()
        {
            Assert.That(new EnemyDefenseStrategy().TrySelect(Snapshot(new ForceComposition(), 2000f, 2200f, Pad()), out _), Is.False);
        }

        [Test]
        public void HostilesOnPad_BlockConstruction()
        {
            Assert.That(new EnemyDefenseStrategy().TrySelect(Snapshot(new ForceComposition(), 5000f, 0f, Pad(hostiles: true)), out _), Is.False);
        }

        [Test]
        public void ThreatenedPoint_CanSpendProductionReserve()
        {
            Assert.That(new EnemyDefenseStrategy().TrySelect(Snapshot(new ForceComposition(), 2000f, 2200f, Pad(threatened: true)), out _), Is.True);
        }

        [Test]
        public void Easy_StopsAtOneSatellitePerPoint()
        {
            Assert.That(new EnemyDefenseStrategy().TrySelect(Snapshot(new ForceComposition(), 5000f, 0f,
                Pad(existing: new[] { SiteFacilityType.IonSatellite }), difficulty: EnemyAiDifficulty.Easy), out _), Is.False);
        }

        [Test]
        public void Choices_AreRankedAcrossPadsAndIncludeTopThree()
        {
            var strategy = new EnemyDefenseStrategy();
            var snapshot = Snapshot(Force(ShipClass.Frigate, 3000f, 1000f), 10000f, 0f, Pad());
            snapshot = new EnemyDefenseSnapshot(snapshot.Money, snapshot.IncomePerSecond, 0f, snapshot.Strategic,
                snapshot.Own, snapshot.Hostile, new[] { Pad(), new EnemyDefensePad(7, 10f, 0.8f, 0f, false, true, Array.Empty<SiteFacilityType>()) }, _options);
            Assert.That(strategy.TrySelect(snapshot, out var choice), Is.True);
            Assert.That(choice.SiteIndex, Is.EqualTo(7));
            Assert.That(strategy.TopChoices.Count, Is.EqualTo(3));
            Assert.That(strategy.TopChoices[0].Score, Is.GreaterThanOrEqualTo(strategy.TopChoices[1].Score));
        }

        private EnemyDefenseSnapshot Snapshot(ForceComposition hostile, float money, float reserve, EnemyDefensePad pad,
            float baseThreat = 0f, EnemyAiDifficulty difficulty = EnemyAiDifficulty.Medium) =>
            new EnemyDefenseSnapshot(money, 5f, reserve,
                new EnemyStrategicSnapshot(default, difficulty, 3, hostile.UnitCount, 0, 1f, baseThreat, false, true, true),
                new ForceComposition(), hostile, new[] { pad }, _options);

        private static EnemyDefensePad Pad(float exposure = 0.5f, bool hostiles = false, bool threatened = false,
            SiteFacilityType[] existing = null) => new EnemyDefensePad(0, 3f, exposure, float.PositiveInfinity,
            hostiles, threatened, existing ?? Array.Empty<SiteFacilityType>());

        private static ForceComposition Force(ShipClass shipClass, float hull, float shields)
        {
            var force = new ForceComposition();
            force.AddNew(new UnitCombatProfile(shipClass, hull, shields, new float[ForceComposition.ClassSlots],
                new float[ForceComposition.ClassSlots], new float[ForceComposition.ClassSlots], Array.Empty<UnitCombatProfile>()), false);
            return force;
        }

        private static string[] Describe(EnemyDefenseStrategy strategy)
        {
            var result = new string[strategy.TopChoices.Count];
            for (int i = 0; i < result.Length; i++) result[i] = $"{strategy.TopChoices[i].Type}: {strategy.TopChoices[i].Score}";
            return result;
        }
    }
}
