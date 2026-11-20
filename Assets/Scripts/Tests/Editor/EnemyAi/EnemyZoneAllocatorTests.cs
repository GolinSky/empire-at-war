using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.EnemyFaction.Models.Objectives;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("EnemyAi")]
    public sealed class EnemyZoneAllocatorTests
    {
        private const int RESERVE = EnemyZoneAllocator.RESERVE;

        [Test]
        public void Allocate_PoolTooSmallForTheZoneStaysTogetherAndGathersForIt()
        {
            EnemyZoneObjective zone = Zone("defended", defenders: 3);
            var allocator = new EnemyZoneAllocator();

            List<int> assignment = Allocate(allocator, new[] { zone }, ships: 2);

            Assert.That(assignment, Is.All.EqualTo(RESERVE), "No ship is sent while the pool cannot beat the zone.");
            Assert.That(allocator.GatherZone, Is.EqualTo(0));
        }

        [Test]
        public void Allocate_ShipsBeyondTheNeededForceJoinItInsteadOfWaiting()
        {
            EnemyZoneObjective zone = Zone("defended", defenders: 3);
            var allocator = new EnemyZoneAllocator();

            List<int> assignment = Allocate(allocator, new[] { zone }, ships: 5);

            Assert.That(assignment, Is.All.EqualTo(0), "No ship waits in an idle reserve.");
            Assert.That(allocator.GatherZone, Is.EqualTo(RESERVE));
        }

        [Test]
        public void Allocate_FreeZonesGetOneShipEachBeforeAnyGetsASecond()
        {
            EnemyZoneObjective[] zones = { Zone("a"), Zone("b") };

            List<int> assignment = Allocate(new EnemyZoneAllocator(), zones, ships: 3);

            Assert.That(assignment.Count(target => target == 0), Is.GreaterThanOrEqualTo(1));
            Assert.That(assignment.Count(target => target == 1), Is.GreaterThanOrEqualTo(1));
            Assert.That(assignment, Has.None.EqualTo(RESERVE));
        }

        [Test]
        public void Allocate_NearestShipIsSent()
        {
            EnemyZoneObjective[] zones = { Zone("a"), Zone("b") };
            float[,] distances = { { 900f, 900f }, { 100f, 800f }, { 800f, 100f } };

            List<int> assignment = Allocate(new EnemyZoneAllocator(), zones, ships: 3,
                distance: (ship, zone) => distances[ship, zone]);

            Assert.That(assignment[1], Is.EqualTo(0));
            Assert.That(assignment[2], Is.EqualTo(1));
        }

        [Test]
        public void Allocate_SentForceKeepsItsZoneUntilItsOwnFightIsLost()
        {
            EnemyZoneObjective zone = Zone("defended", defenders: 20);
            var allocator = new EnemyZoneAllocator();

            // 15 of 20 is below an even fight; a zone recount alone does not recall the force.
            List<int> kept = Allocate(allocator, new[] { zone }, ships: 15, current: 0);

            Assert.That(kept, Is.All.EqualTo(0));
        }

        [Test]
        public void Allocate_NothingBeatable_PoolAttacksTheWeakTargetTogether()
        {
            EnemyZoneObjective[] zones = { Zone("strong", defenders: 9), Zone("weaker", defenders: 4) };
            var allocator = new EnemyZoneAllocator { WeakTarget = 1 };

            List<int> assignment = Allocate(allocator, zones, ships: 3);

            Assert.That(assignment, Is.All.EqualTo(1));
        }

        [Test]
        public void Allocate_CentreForceIsLedByTheStrongestFreeShip()
        {
            EnemyZoneObjective[] zones = { Zone("outer"), Zone("centre", centre: true) };
            var forces = new List<ForceComposition>();
            for (int i = 0; i < 3; i++)
            {
                var force = new ForceComposition();
                AddShips(force, 1, hull: i == 2 ? 5000f : 1000f);
                forces.Add(force);
            }
            var assignment = new List<int>();

            // Ship 0 is nearest everywhere; the strongest (ship 2) must still lead the centre.
            new EnemyZoneAllocator().Allocate(zones, forces, new[] { RESERVE, RESERVE, RESERVE },
                (ship, zone) => ship, false, assignment);

            Assert.That(assignment[2], Is.EqualTo(1));
        }

        [Test]
        public void Tier_CentreIsNeverExcludedAndRisesWithAStrongEconomy()
        {
            EnemyZoneObjective centre = Zone("centre", centre: true);

            Assert.That(EnemyZoneEvaluator.GetTier(centre, EnemyZoneVerdict.Assemble, behindOnZones: true),
                Is.EqualTo(4), "Weak: the centre is the last target, but still a target.");
            Assert.That(EnemyZoneEvaluator.GetTier(centre, EnemyZoneVerdict.Assemble, true, strongEconomy: true),
                Is.LessThan(4));
            Assert.That(EnemyZoneEvaluator.GetTier(centre, EnemyZoneVerdict.Go, behindOnZones: true),
                Is.LessThan(4), "A centre the force beats (its defenders left) is taken.");
        }

        [Test]
        public void Assess_OwnSatellitesInTheZoneFightBesideTheForce()
        {
            EnemyZoneObjective zone = Zone("held", defenders: 2);
            var force = new ForceComposition();
            AddShips(force, 1);
            var evaluator = new EnemyZoneEvaluator();

            EnemyZoneVerdict alone = evaluator.Assess(zone, force, EnemyZoneEvaluator.COMMIT_ADVANTAGE).Verdict;
            AddShips(zone.OwnSupport, 2);
            EnemyZoneVerdict supported = evaluator.Assess(zone, force, EnemyZoneEvaluator.COMMIT_ADVANTAGE).Verdict;

            Assert.That(alone, Is.EqualTo(EnemyZoneVerdict.Assemble));
            Assert.That(supported, Is.EqualTo(EnemyZoneVerdict.Go));
        }

        [Test]
        public void Allocate_KeptForceBelowAnEvenFightIsToppedUpFromTheReserve()
        {
            EnemyZoneObjective zone = Zone("defended", defenders: 10);
            var current = new List<int>();
            for (int i = 0; i < 9; i++) current.Add(0);
            current.Add(RESERVE);

            List<int> assignment = Allocate(new EnemyZoneAllocator(), new[] { zone }, ships: 10, currentPerShip: current);

            Assert.That(assignment, Is.All.EqualTo(0));
        }

        [Test]
        public void Allocate_DeniedZoneGetsNoShipsAndIsNoGatheringTarget()
        {
            EnemyZoneObjective zone = Zone("base", denyReason: "hostileBase");
            var allocator = new EnemyZoneAllocator();

            List<int> assignment = Allocate(allocator, new[] { zone }, ships: 4, current: 0);

            Assert.That(assignment, Is.All.EqualTo(RESERVE));
            Assert.That(allocator.GatherZone, Is.EqualTo(RESERVE));
        }

        [Test]
        public void Allocate_ThreatenedOwnZoneIsServedBeforeARicherFreeZone()
        {
            EnemyZoneObjective[] zones = { Zone("rich", priority: 5000f), Zone("threatened", priority: 0f, threatened: true) };

            List<int> assignment = Allocate(new EnemyZoneAllocator(), zones, ships: 1);

            Assert.That(assignment.Single(), Is.EqualTo(1));
        }

        [Test]
        public void Allocate_BehindOnZones_OwnSideZoneIsServedBeforeTheEnemySide()
        {
            EnemyZoneObjective[] zones = { Zone("enemySide", priority: 1000f), Zone("ownSide", priority: 100f, ownSide: true) };

            List<int> behind = Allocate(new EnemyZoneAllocator(), zones, ships: 1, behindOnZones: true);
            List<int> ahead = Allocate(new EnemyZoneAllocator(), zones, ships: 1);

            Assert.That(behind.Single(), Is.EqualTo(1));
            Assert.That(ahead.Single(), Is.EqualTo(0));
        }

        [Test]
        public void Tier_HeldZoneUnderAttackIsTheTopTierEvenBelowAnEvenFight()
        {
            EnemyZoneObjective held = Zone("held", defenders: 5, held: true, ownStructures: 1);
            EnemyZoneObjective quiet = Zone("quiet", held: true, ownStructures: 1);

            Assert.That(EnemyZoneEvaluator.GetTier(held, EnemyZoneVerdict.Assemble, behindOnZones: true), Is.EqualTo(-1));
            Assert.That(EnemyZoneEvaluator.GetTier(quiet, EnemyZoneVerdict.Go, behindOnZones: true), Is.GreaterThan(-1),
                "A held zone without hostiles needs no defence.");
        }

        [Test]
        public void Allocate_HeldZoneWithOwnSatellitesGetsShipsBelowAnEvenFight_ARelayAloneDoesNot()
        {
            EnemyZoneObjective satellites = Zone("satellites", defenders: 3, held: true, ownStructures: 2);
            EnemyZoneObjective relayOnly = Zone("relayOnly", defenders: 3, held: true);
            EnemyZoneObjective overrun = Zone("overrun", defenders: 10, held: true, ownStructures: 2);

            List<int> defended = Allocate(new EnemyZoneAllocator(), new[] { satellites }, ships: 2);
            List<int> gathered = Allocate(new EnemyZoneAllocator(), new[] { relayOnly }, ships: 2);
            List<int> spared = Allocate(new EnemyZoneAllocator(), new[] { overrun }, ships: 2);

            Assert.That(defended, Is.All.EqualTo(0), "Ships fight around their own satellites.");
            Assert.That(gathered, Is.All.EqualTo(RESERVE));
            Assert.That(spared, Is.All.EqualTo(RESERVE),
                "Far below the defend floor the ships gather instead of dying one by one.");
        }

        [Test]
        public void Allocate_LeftoverShipJoinsOnlyAForceItKeepsWinning()
        {
            EnemyZoneObjective losing = Zone("losing", defenders: 10);
            var current = new List<int> { 0, 0, RESERVE };

            List<int> assignment = Allocate(new EnemyZoneAllocator(), new[] { losing }, ships: 3, currentPerShip: current);

            Assert.That(assignment[0], Is.EqualTo(0), "A kept force keeps its zone.");
            Assert.That(assignment[2], Is.EqualTo(RESERVE), "A free ship is not fed alone into a losing fight.");
        }

        [Test]
        public void Allocate_DeployZoneIsTheGatherZone_ElseTheBestServedZone()
        {
            EnemyZoneObjective[] zones = { Zone("free", priority: 100f), Zone("defended", defenders: 5, priority: 900f) };
            var gathering = new EnemyZoneAllocator();
            var served = new EnemyZoneAllocator();

            Allocate(gathering, zones, ships: 1);
            Allocate(served, new[] { zones[0] }, ships: 1);

            Assert.That(gathering.GatherZone, Is.EqualTo(1));
            Assert.That(gathering.DeployZone, Is.EqualTo(1), "New ships are needed where the next force gathers.");
            Assert.That(served.DeployZone, Is.EqualTo(0), "With every zone served they go to the best-ranked force.");
        }

        private static List<int> Allocate(EnemyZoneAllocator allocator, IReadOnlyList<EnemyZoneObjective> zones, int ships,
            int current = RESERVE, List<int> currentPerShip = null, Func<int, int, float> distance = null,
            bool behindOnZones = false)
        {
            var forces = new List<ForceComposition>();
            var currentZones = currentPerShip ?? new List<int>();
            for (int i = 0; i < ships; i++)
            {
                var force = new ForceComposition();
                AddShips(force, 1);
                forces.Add(force);
                if (currentPerShip == null) currentZones.Add(current);
            }
            var assignment = new List<int>();
            allocator.Allocate(zones, forces, currentZones, distance ?? ((ship, zone) => ship), behindOnZones, assignment);
            return assignment;
        }

        private static EnemyZoneObjective Zone(string id, int defenders = 0, float priority = 500f,
            string denyReason = null, bool threatened = false, bool ownSide = false, bool centre = false,
            bool held = false, int ownStructures = 0)
        {
            var zone = new EnemyZoneObjective();
            zone.Reset(id, centre, ownSide, priority, 30f, 0, denyReason, threatened, held, ownStructures);
            AddShips(zone.Defenders, defenders);
            return zone;
        }

        // Equal ships on both sides: N ships against M defenders is an N / M advantage.
        private static void AddShips(ForceComposition force, int ships, float hull = 1000f)
        {
            float[] rates = new float[ForceComposition.ClassSlots];
            for (int i = 0; i < rates.Length; i++) rates[i] = 50f;
            var profile = new UnitCombatProfile(ShipClass.Frigate, hull, 0f, rates, rates,
                new float[ForceComposition.ClassSlots], Array.Empty<UnitCombatProfile>());
            for (int i = 0; i < ships; i++) force.AddNew(profile, false);
        }
    }
}
