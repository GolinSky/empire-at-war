using System;
using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.Enemy;
using NUnit.Framework;
using UnityEditor;

namespace EmpireAtWar.Tests.Editor
{
    [Category("EnemyAi")]
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
        // Heavy caliber damage ×1.3 (aae27525d) made heavy missiles outscore mass drivers against besieging capitals.
        [TestCase(ShipClass.Capital, 10000f, 30000f, 12000f, 0.1f, 2f, SiteFacilityType.MissileSatellite)]
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
            Assert.That(strategy.TrySelectScored(snapshot, out var choice), Is.True);
            Assert.That(choice.Type, Is.EqualTo(expected), string.Join("; ", Describe(strategy)));
        }

        [Test]
        public void NoIntel_SelectsGeneralist()
        {
            var strategy = new EnemyDefenseStrategy();
            Assert.That(strategy.TrySelectScored(Snapshot(new ForceComposition(), 2000f, 0f, Pad()), out var choice), Is.True);
            Assert.That(choice.Type, Is.EqualTo(SiteFacilityType.TurbolaserSatellite));
        }

        [Test]
        public void ProductionReserve_BlocksPeacetimeSpending()
        {
            Assert.That(new EnemyDefenseStrategy().TrySelectScored(Snapshot(new ForceComposition(), 2000f, 2200f, Pad()), out _), Is.False);
        }

        [Test]
        public void HostilesOnPad_BlockConstruction()
        {
            Assert.That(new EnemyDefenseStrategy().TrySelectScored(Snapshot(new ForceComposition(), 5000f, 0f, Pad(hostiles: true)), out _), Is.False);
        }

        [Test]
        public void ThreatenedPoint_CanSpendProductionReserve()
        {
            Assert.That(new EnemyDefenseStrategy().TrySelectScored(Snapshot(new ForceComposition(), 2000f, 2200f, Pad(threatened: true)), out _), Is.True);
        }

        [TestCase(EnemyAiDifficulty.Easy)]
        [TestCase(EnemyAiDifficulty.Medium)]
        [TestCase(EnemyAiDifficulty.Hard)]
        [TestCase(EnemyAiDifficulty.UltraHard)]
        public void OptionalDefense_UsesTheSameSatelliteBudgetAtEveryDifficulty(EnemyAiDifficulty difficulty)
        {
            Assert.That(new EnemyDefenseStrategy().TrySelectScored(Snapshot(new ForceComposition(), 5000f, 0f,
                Pad(existing: new[] { SiteFacilityType.IonSatellite }), difficulty: difficulty), out _), Is.True);
        }

        [Test]
        public void Choices_AreRankedAcrossPadsAndIncludeTopThree()
        {
            var strategy = new EnemyDefenseStrategy();
            var snapshot = Snapshot(Force(ShipClass.Frigate, 3000f, 1000f), 10000f, 0f, Pad());
            snapshot = new EnemyDefenseSnapshot(snapshot.Money, snapshot.IncomePerSecond, 0f, snapshot.Strategic,
                snapshot.Own, snapshot.Hostile, new[] { Pad(), new EnemyDefensePad(7, 1f, 0.8f, 0f, false, true, Array.Empty<SiteFacilityType>()) }, _options);
            Assert.That(strategy.TrySelectScored(snapshot, out var choice), Is.True);
            Assert.That(choice.SiteIndex, Is.EqualTo(7));
            Assert.That(strategy.TopChoices.Count, Is.EqualTo(3));
            Assert.That(strategy.TopChoices[0].Score, Is.GreaterThanOrEqualTo(strategy.TopChoices[1].Score));
        }

        [Test]
        public void Mandatory_ZonePadGetsThreeTurbolasersEvenUnderProductionReserve()
        {
            var strategy = new EnemyDefenseStrategy();
            var zone = new EnemyDefensePad(3, 0f, 0.5f, float.PositiveInfinity, false, false,
                Array.Empty<SiteFacilityType>(), isZonePad: true);
            var snapshot = Snapshot(new ForceComposition(), 2000f, 5000f, zone);

            Assert.That(strategy.TrySelect(snapshot, out var choice), Is.True);
            Assert.That(choice.Type, Is.EqualTo(SiteFacilityType.TurbolaserSatellite));
            Assert.That(choice.SiteIndex, Is.EqualTo(3));

            var full = new EnemyDefensePad(3, 0f, 0.5f, float.PositiveInfinity, false, false,
                new[] { SiteFacilityType.TurbolaserSatellite, SiteFacilityType.TurbolaserSatellite, SiteFacilityType.TurbolaserSatellite },
                isZonePad: true);
            Assert.That(strategy.TrySelect(Snapshot(new ForceComposition(), 2000f, 5000f, full), out _), Is.False);
        }

        [Test]
        public void Mandatory_MiningPointGetsOneSatelliteAndQuadLaserAgainstSquadrons()
        {
            var strategy = new EnemyDefenseStrategy();
            var snapshot = Snapshot(Force(ShipClass.Fighter, 600f, 0f), 2000f, 5000f, Pad());

            Assert.That(strategy.TrySelect(snapshot, out var choice), Is.True);
            Assert.That(choice.Type, Is.EqualTo(SiteFacilityType.QuadLaserSatellite));
        }

        [Test]
        public void Mandatory_RichPointAddsAHeavySatelliteAfterTheFirst()
        {
            var strategy = new EnemyDefenseStrategy();
            var zone = new EnemyDefensePad(3, 0f, 0.5f, float.PositiveInfinity, false, false,
                new[] { SiteFacilityType.TurbolaserSatellite }, isZonePad: true);
            var snapshot = Snapshot(Force(ShipClass.Capital, 10000f, 30000f), 20000f, 0f, zone);

            Assert.That(strategy.TrySelect(snapshot, out var choice), Is.True);
            Assert.That(choice.Type, Is.EqualTo(SiteFacilityType.MissileSatellite).Or.EqualTo(SiteFacilityType.MassDriverSatellite));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Construction_AttemptsCentreSatelliteBeforeMiningThenLeavesOptionalForLater(bool satelliteCanStart)
        {
            var strategy = new EnemyDefenseStrategy();
            var construction = new ConstructionStub(satelliteCanStart);
            var roster = TestPlayers.CreateDuel();
            var controller = new EnemySiteConstructionController(null, null, construction,
                roster.Get(TestPlayers.Enemy), null, strategy, null, null, null);
            var centre = new EnemyDefensePad(2, 0f, 0.5f, float.PositiveInfinity, false, false,
                Array.Empty<SiteFacilityType>(), isCentrePad: true);
            var snapshot = new EnemyDefenseSnapshot(2000f, 5f, 0f, default, new ForceComposition(),
                new ForceComposition(), new[] { Pad(), centre }, _options);

            controller.BuildNext(snapshot, mineSite: 3, mandatorySatellite: true);

            Assert.That(construction.Started, Is.EqualTo(satelliteCanStart ? new[] { 2 } : new[] { 2, 3 }));
        }

        [Test]
        public void Construction_FillsAnUnbuiltMineBeforeOptionalDefense()
        {
            var strategy = new EnemyDefenseStrategy();
            var construction = new ConstructionStub(true);
            var roster = TestPlayers.CreateDuel();
            var controller = new EnemySiteConstructionController(null, null, construction,
                roster.Get(TestPlayers.Enemy), null, strategy, null, null, null);
            var snapshot = Snapshot(Force(ShipClass.Cruiser, 5000f, 15000f), 2000f, 0f,
                Pad(threatened: true, existing: new[] { SiteFacilityType.TurbolaserSatellite }));
            Assert.That(strategy.TrySelectScored(snapshot, out var optional), Is.True);

            controller.BuildNext(snapshot, mineSite: 3, mandatorySatellite: false);
            controller.BuildNext(snapshot, mineSite: -1, mandatorySatellite: false);

            Assert.That(construction.Started, Is.EqualTo(new[] { 3, optional.SiteIndex }));
        }

        private sealed class ConstructionStub : ICaptureSiteConstruction
        {
            private readonly bool _startSatellite;
            public List<int> Started { get; } = new List<int>();

            public ConstructionStub(bool startSatellite) => _startSatellite = startSatellite;

            public bool TryStartConstruction(int siteIndex, PlayerId owner, SiteFacilityType type)
            {
                Started.Add(siteIndex);
                return type == SiteFacilityType.Mining || _startSatellite;
            }
        }

        private EnemyDefenseSnapshot Snapshot(ForceComposition hostile, float money, float reserve, EnemyDefensePad pad,
            float baseThreat = 0f, EnemyAiDifficulty difficulty = EnemyAiDifficulty.Medium) =>
            new EnemyDefenseSnapshot(money, 5f, reserve,
                new EnemyStrategicSnapshot(default, difficulty, 3, hostile.UnitCount, 0, 1f, baseThreat, false, true, true),
                new ForceComposition(), hostile, new[] { pad }, _options);

        private static EnemyDefensePad Pad(float exposure = 0.5f, bool hostiles = false, bool threatened = false,
            SiteFacilityType[] existing = null) => new EnemyDefensePad(0, 1f, exposure, float.PositiveInfinity,
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
