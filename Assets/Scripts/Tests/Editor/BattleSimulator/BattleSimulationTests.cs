using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Editor.BattleSimulator;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class BattleSimulationTests
    {
        private DamageMatrixData _matrix;

        [SetUp]
        public void SetUp()
        {
            _matrix = ScriptableObject.CreateInstance<DamageMatrixData>();
            JsonUtility.FromJsonOverwrite(@"{""damageTypes"":[
                {""damageType"":2,""damage"":{""capital"":1,""fighter"":1},""accuracy"":{""capital"":1,""fighter"":1},""vsShield"":1},
                {""damageType"":6,""damage"":{""capital"":1,""fighter"":1},""accuracy"":{""capital"":1,""fighter"":1},""vsShield"":1,""shieldPiercing"":true}]}", _matrix);
        }

        [TearDown] public void TearDown() => UnityEngine.Object.DestroyImmediate(_matrix);

        [Test]
        public void AnalyticTtk_TenTenDamageShots_OneSecondFlight_KillsAtTenSeconds()
        {
            var setup = Setup();
            setup.Distance = 10;
            BattleResult result = Run(setup, Unit("A", 1000, 10), Unit("B", 100, 0));
            // Shots at 0..9 s arrive at 1..10 s: 100 hull / 10 DPS = 10 s.
            Assert.That(result.Duration, Is.EqualTo(10).Within(BattleSimulation.STEP_SECONDS));
            Assert.That(result.Winner, Is.Zero);
            Assert.That(result.Units[0].DamageDealt, Is.EqualTo(100));
        }

        [Test]
        public void AlphaStrike_NoTravel_UsesDiscreteShotsRatherThanContinuousDps()
        {
            BattleResult result = Run(Setup(), Unit("A", 1000, 10), Unit("B", 100, 0));
            Assert.That(result.Duration, Is.EqualTo(9).Within(0.0001));
        }

        [Test]
        public void SameSeed_RepeatsTimelineEventsAndStatistics()
        {
            var setup = Setup(); setup.AlphaStrike = false;
            var a = Unit("A", 100, 17); var b = Unit("B", 100, 13);
            var first = Run(setup, a, b); var second = Run(setup, a, b);
            Assert.That(second.Duration, Is.EqualTo(first.Duration));
            Assert.That(second.Winner, Is.EqualTo(first.Winner));
            Assert.That(second.Timeline.Select(s => (s.Time, s.HullA, s.HullB)), Is.EqualTo(first.Timeline.Select(s => (s.Time, s.HullA, s.HullB))));
            Assert.That(second.Events.Select(e => (e.Time, e.UnitIndex, e.Kind)), Is.EqualTo(first.Events.Select(e => (e.Time, e.UnitIndex, e.Kind))));
        }

        [Test]
        public void SimultaneousLethalShots_ResultInMutualDraw()
        {
            BattleResult result = Run(Setup(), Unit("A", 10, 10), Unit("B", 10, 10));
            Assert.That(result.Winner, Is.EqualTo(-1));
            Assert.That(result.Reason, Is.EqualTo("Mutual destruction"));
        }

        [Test]
        public void UnarmedUnits_Timeout()
        {
            var setup = Setup(); setup.TimeLimit = 2;
            Assert.That(Run(setup, Unit("A", 100, 0), Unit("B", 100, 0)).Reason, Is.EqualTo("Timeout"));
        }

        [Test]
        public void PointDefense_DoesNotShootCapitals()
        {
            var a = Unit("A", 100, 10, pointDefense: true);
            var setup = Setup(); setup.TimeLimit = 2;
            Assert.That(Run(setup, a, Unit("B", 100, 0)).Units[0].Shots, Is.Zero);
        }

        [Test]
        public void ShieldRegeneration_UsesRuntimeInitialReadyTimer()
        {
            var b = Unit("B", 100, 0); b.Shields = 30; b.ShieldRegenerateValue = 5;
            var setup = Setup(); setup.TimeLimit = 0.05f;
            BattleResult result = Run(setup, Unit("A", 100, 10), b);
            Assert.That(result.Units[1].Shields, Is.EqualTo(25));
            Assert.That(result.Units[1].Hull, Is.EqualTo(100));
        }

        [Test]
        public void Piercing_DestroysGeneratorAndPreventsRegeneration()
        {
            var a = Unit("A", 100, 10, type: DamageType.ProtonTorpedo);
            var b = Unit("B", 100, 0); b.Shields = 30; b.ShieldRegenerateValue = 5;
            b.HardPoints[0].Type = HardPointType.ShieldGenerator;
            b.HardPointHealth = new[] { Health(HardPointType.ShieldGenerator, 10, 1) };
            var setup = Setup(); setup.TimeLimit = 0.05f;
            BattleResult result = Run(setup, a, b);
            Assert.That(result.Units[1].Hull, Is.EqualTo(90));
            Assert.That(result.Units[1].Shields, Is.Zero);
            Assert.That(result.Events.Any(e => e.Kind == "Shield generator lost"), Is.True);
        }

        [Test]
        public void DestroyedMount_StopsSubsequentShots()
        {
            var b = Unit("B", 1000, 1); b.HardPointHealth = new[] { Health(HardPointType.Weapon, 10, 1) };
            var setup = Setup(); setup.TimeLimit = 3;
            var result = Run(setup, Unit("A", 1000, 10), b);
            Assert.That(result.Units[1].Shots, Is.EqualTo(1));
            Assert.That(result.Units[1].Hull, Is.EqualTo(960));
        }

        [Test]
        public void LockedMount_OnlyFiresAtItsUnlockLevel()
        {
            var a = Unit("A", 100, 10); a.HardPoints[0].UnlockLevel = 2; a.Mounts[0].UnlockLevel = 2;
            var setup = Setup(); setup.TimeLimit = 1;
            Assert.That(Run(setup, a, Unit("B", 100, 0)).Units[0].Shots, Is.Zero);
            setup.SideA[0].Level = 2;
            Assert.That(Run(setup, a, Unit("B", 100, 0)).Units[0].Shots, Is.EqualTo(2));
        }

        [Test]
        public void Salvo_UsesShotIntervalAndStartToStartCycle()
        {
            var a = Unit("A", 100, 10); a.Mounts[0].Profile = Profile(10, 3, 0.2f, 1);
            var setup = Setup(); setup.TimeLimit = 1.4f;
            Assert.That(Run(setup, a, Unit("B", 1000, 0)).Units[0].Shots, Is.EqualTo(6));
        }

        [Test]
        public void SquadronMemberLoss_DisablesOnlyThatMembersWeapons()
        {
            var b = Unit("B", 100, 1); b.IsSquadron = true; b.MemberHull = 10;
            b.ShipClass = ShipClass.Fighter;
            var setup = Setup(); setup.TimeLimit = 3;
            var result = Run(setup, Unit("A", 100, 10), b);
            Assert.That(result.Units[1].DestroyedAt, Is.Zero);
        }

        private BattleResult Run(BattleSetup setup, params SimUnitDefinition[] units)
        {
            var simulation = new BattleSimulation(setup, units.ToDictionary(unit => unit.Id), _matrix, setup.Seed);
            while (!simulation.IsComplete) simulation.Step();
            return simulation.Result;
        }

        [Test]
        public void Beam_SplitsDamageUsingRuntimeTickGranularity()
        {
            var a = Unit("A", 100, 20);
            a.Mounts[0].IsBeam = true; a.Mounts[0].BeamDelay = 0.3f; a.Mounts[0].DamageDuration = 0.25f;
            var result = Run(Setup(), a, Unit("B", 19, 0));
            Assert.That(result.Duration, Is.EqualTo(0.5).Within(0.0001));
            Assert.That(result.Units[0].Shots, Is.EqualTo(1));
        }

        [Test]
        public void ZeroBearingMounts_DealNoDamage()
        {
            var setup = Setup(); setup.TimeLimit = 1; setup.SideA[0].ArcFactor = 0;
            Assert.That(Run(setup, Unit("A", 100, 20), Unit("B", 100, 0)).Units[0].Shots, Is.Zero);
        }

        public static BattleSetup Setup() => new BattleSetup { AlphaStrike = true, Distance = 0,
            SideA = new List<BattleFleetEntry> { new BattleFleetEntry { UnitId = "A" } },
            SideB = new List<BattleFleetEntry> { new BattleFleetEntry { UnitId = "B" } } };

        public static SimUnitDefinition Unit(string id, float hull, float damage, bool pointDefense = false, DamageType type = DamageType.Turbolaser)
        {
            var unit = new SimUnitDefinition { Id = id, Name = id, Hull = hull, ShipClass = ShipClass.Capital,
                ShieldRegenerateDelay = 1, HardPointHealth = new[] { Health(HardPointType.Weapon, 10000, 1) } };
            unit.HardPoints.Add(new SimHardPointDefinition { Name = "Weapon", Type = HardPointType.Weapon });
            if (damage > 0) unit.Mounts.Add(new SimMountDefinition { HardPointId = 0, Profile = Profile(damage, pointDefense: pointDefense, type: type) });
            return unit;
        }

        public static WeaponProfile Profile(float damage, int shots = 1, float interval = 0, float reload = 1,
            bool pointDefense = false, DamageType type = DamageType.Turbolaser) => new WeaponProfile(WeaponType.TurboLaser,
            "Test", WeaponFamily.Turbolaser, type, damage, shots, interval, reload, 10, pointDefense, false,
            null, 10, Color.white, Vector3.one, false, false, default, default, 1, 0);

        public static HardPointHealth Health(HardPointType type, float health, float multiplier) => JsonUtility.FromJson<HardPointHealth>(
            "{\"hardPointType\":" + (int)type + ",\"health\":" + health.ToString(System.Globalization.CultureInfo.InvariantCulture) +
            ",\"hullDamageMultiplier\":" + multiplier.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}");
    }
}
