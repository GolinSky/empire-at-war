using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Services.Enemy;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.OwnedAreas;
using EmpireAtWar.Services.SpawnBlocking;
using EmpireAtWar.Services.Vision;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("EnemyAi")]
    public sealed class EnemyPatrolMemoryTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void StalledPatrolSwitchesOrReleasesAndDoesNotImmediatelyRetry(bool hasAlternative)
        {
            var roster = TestPlayers.CreateDuel();
            var owner = roster.Get(TestPlayers.Enemy);
            var registry = new TeamIntelRegistry();
            var patrol = new EnemyPatrolService(registry, owner, null, null, null,
                new VisionService(roster), new OpenAreas(), null, roster);
            patrol.Memory.NextSampleTime = float.PositiveInfinity;
            patrol.Memory.Register("stalled", new FormationPoint(10f, 0f), "sector", 1f);
            if (hasAlternative) patrol.Memory.Register("other", new FormationPoint(100f, 0f), "sector", 1f);
            Assert.That(patrol.TryGetTarget("unit", Vector3.zero, _ => true, out var first), Is.True);
            Assert.That(first.x, Is.EqualTo(10f));
            var progress = (Dictionary<string, (float Distance, float At)>)typeof(EnemyPatrolService)
                .GetField("_progress", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(patrol);
            progress["unit"] = (10f, Time.time - 31f);

            Assert.That(patrol.TryGetTarget("unit", Vector3.zero, _ => true, out var next), Is.EqualTo(hasAlternative));
            if (hasAlternative) Assert.That(next.x, Is.EqualTo(100f));
            Assert.That(patrol.TryGetTarget("unit", Vector3.zero, _ => true, out var retry), Is.EqualTo(hasAlternative));
            if (hasAlternative) Assert.That(retry.x, Is.EqualTo(100f));
        }

        [Test]
        public void UnseenSectorBeatsRecentlyObservedSectorAtEqualDistance()
        {
            var memory = new EnemyPatrolMemory();
            memory.Register("seen", new FormationPoint(10f, 0f), "sector", 1f);
            memory.Register("unseen", new FormationPoint(-10f, 0f), "sector", 1f);
            memory.Observe("seen", 29f);

            Assert.That(memory.Select(default, 30f, _ => true, out _).Id, Is.EqualTo("unseen"));
        }

        [Test]
        public void AlliedMineReturnsToTopAfterItBecomesStale()
        {
            var memory = new EnemyPatrolMemory();
            memory.Register("mine", new FormationPoint(10f, 0f), "alliedMine", 5f);
            memory.Register("sector", new FormationPoint(-10f, 0f), "sector", 1f);
            memory.Observe("mine", 30f);
            Assert.That(memory.Select(default, 30f, _ => true, out _).Id, Is.EqualTo("sector"));

            memory.Observe("sector", 100f);
            Assert.That(memory.Select(default, 100f, _ => true, out _).Id, Is.EqualTo("mine"));
        }

        [Test]
        public void BlockedOrUnsafePointsAreExcludedEvenWhenMoreValuable()
        {
            var memory = new EnemyPatrolMemory();
            memory.Register("blocked", new FormationPoint(1f, 0f), "alliedMine", 5f);
            memory.Register("unsafe", new FormationPoint(2f, 0f), "mine", 3f);
            memory.Register("safe", new FormationPoint(20f, 0f), "sector", 1f);

            Assert.That(memory.Select(default, 100f, point => point.Id == "safe", out _).Id, Is.EqualTo("safe"));
        }

        [Test]
        public void StalePatrolAndIntelUseTheSameScore()
        {
            var memory = new EnemyPatrolMemory();
            memory.Register("sector", new FormationPoint(10f, 0f), "sector", 1f);
            memory.Register("intel", new FormationPoint(-10f, 0f), "intel", 1f);
            memory.Observe("intel", 90f);
            Assert.That(memory.Select(default, 100f, _ => true, out _).Id, Is.EqualTo("sector"));

            memory.Observe("sector", 100f);
            Assert.That(memory.Select(default, 100f, _ => true, out _).Id, Is.EqualTo("intel"));
        }

        [Test]
        public void RefreshingPointMetadataPreservesVisitTimeAndExpiredIntelCanBeRemoved()
        {
            var memory = new EnemyPatrolMemory();
            memory.Register("intel:1", default, "intel", 1f);
            memory.Observe("intel:1", 12f);
            var point = memory.Register("intel:1", new FormationPoint(5f, 4f), "hostileStructure", 2f);
            Assert.That(point.LastSeenTime, Is.EqualTo(12f));
            memory.Remove("intel:1");
            Assert.That(memory.Entries, Is.Empty);
        }

        [TestCase(EnemyAiDifficulty.Easy, 3, false, false)]
        [TestCase(EnemyAiDifficulty.Medium, 3, false, true)]
        [TestCase(EnemyAiDifficulty.Hard, 3, true, false)]
        [TestCase(EnemyAiDifficulty.Medium, 1, false, false)]
        public void PatrolShipYieldsOnlyToCriticalStationOrLastShip(EnemyAiDifficulty difficulty, int ships,
            bool stationCritical, bool expected)
        {
            Assert.That(EnemyPatrolShipController.CanPatrol(difficulty, ships, stationCritical), Is.EqualTo(expected));
        }

        private sealed class OpenAreas : IBattleAreaService
        {
            public IReadOnlyList<OwnedCircle> Get(BattleAreaLayer layer) => Array.Empty<OwnedCircle>();
            public bool IsBlocked(PlayerId team, Vector3 position) => false;
            public bool IsStructureExcluded(Vector3 position, float margin) => false;
            public bool TryGetOrderSnap(Vector3 position, out Vector3 center) { center = default; return false; }
        }
    }
}
