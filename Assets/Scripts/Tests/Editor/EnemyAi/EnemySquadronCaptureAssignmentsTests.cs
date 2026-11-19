using System;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Services.Enemy;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("EnemyAi")]
    public sealed class EnemySquadronCaptureAssignmentsTests
    {
        [Test]
        public void Assign_SpreadsNearbySquadronsAcrossObjectives()
        {
            var assignments = new EnemySquadronCaptureAssignments();
            var first = new Squadron(Vector3.zero);
            var second = new Squadron(Vector3.zero);
            assignments.AddTarget(Vector3.right);
            assignments.AddTarget(Vector3.right * 100f);

            assignments.Assign(new[] { first, second });

            Assert.That(assignments.TryGetTarget(first, out Vector3 firstTarget), Is.True);
            Assert.That(assignments.TryGetTarget(second, out Vector3 secondTarget), Is.True);
            Assert.That(firstTarget, Is.Not.EqualTo(secondTarget));
        }

        [Test]
        public void Assign_RetainsValidTargetsWhenSquadronOrderChanges()
        {
            var assignments = new EnemySquadronCaptureAssignments();
            var first = new Squadron(Vector3.zero);
            var second = new Squadron(Vector3.zero);
            assignments.AddTarget(Vector3.right);
            assignments.AddTarget(Vector3.left);
            assignments.Assign(new[] { first, second });
            assignments.TryGetTarget(first, out Vector3 original);

            assignments.ClearTargets();
            assignments.AddTarget(Vector3.right);
            assignments.AddTarget(Vector3.left);
            assignments.Assign(new[] { second, first });

            assignments.TryGetTarget(first, out Vector3 retained);
            Assert.That(retained, Is.EqualTo(original));
        }

        [Test]
        public void Assign_ReleasesUnavailableSquadronsAndCapturedTargets()
        {
            var assignments = new EnemySquadronCaptureAssignments();
            var first = new Squadron(Vector3.zero);
            var second = new Squadron(Vector3.zero);
            assignments.AddTarget(Vector3.right);
            assignments.Assign(new[] { first, second });

            assignments.ClearTargets();
            assignments.AddTarget(Vector3.left);
            assignments.Assign(new[] { second });

            Assert.That(assignments.TryGetTarget(first, out _), Is.False);
            Assert.That(assignments.TryGetTarget(second, out Vector3 target), Is.True);
            Assert.That(target, Is.EqualTo(Vector3.left));
        }

        [Test]
        public void Assign_NewThreatGetsDefenderWhileOtherSquadronKeepsCapturing()
        {
            var assignments = new EnemySquadronCaptureAssignments();
            var first = new Squadron(Vector3.zero);
            var second = new Squadron(Vector3.right * 100f);
            assignments.AddTarget(Vector3.right);
            assignments.AddTarget(Vector3.right * 101f);
            assignments.Assign(new[] { first, second });

            assignments.ClearTargets();
            assignments.AddTarget(Vector3.right);
            assignments.AddTarget(Vector3.right * 101f);
            assignments.AddTarget(Vector3.left, threatened: true);
            assignments.Assign(new[] { first, second });

            assignments.TryGetTarget(first, out Vector3 defense);
            assignments.TryGetTarget(second, out Vector3 capture);
            Assert.That(defense, Is.EqualTo(Vector3.left));
            Assert.That(capture, Is.EqualTo(Vector3.right * 101f));
        }

        [Test]
        public void Assign_ExtraSquadronsShareTargetsOnlyAfterEachHasCoverage()
        {
            var assignments = new EnemySquadronCaptureAssignments();
            var squadrons = new[] { new Squadron(Vector3.zero), new Squadron(Vector3.zero),
                new Squadron(Vector3.zero), new Squadron(Vector3.zero) };
            assignments.AddTarget(Vector3.right);
            assignments.AddTarget(Vector3.right * 100f);
            assignments.Assign(squadrons);

            int nearby = 0;
            foreach (Squadron squadron in squadrons)
            {
                assignments.TryGetTarget(squadron, out Vector3 target);
                if (target == Vector3.right) nearby++;
            }
            Assert.That(nearby, Is.EqualTo(2));
        }

        [Test]
        public void Release_RemovesAssignmentBeforePoolReuse()
        {
            var assignments = new EnemySquadronCaptureAssignments();
            var squadron = new Squadron(Vector3.zero);
            assignments.AddTarget(Vector3.right);
            assignments.Assign(new[] { squadron });

            assignments.Release(squadron);

            Assert.That(assignments.TryGetTarget(squadron, out _), Is.False);
        }

        [Test]
        public void Assign_ContestedSiteRequiresEnoughWingsToWin()
        {
            var assignments = new EnemySquadronCaptureAssignments();
            var site = Vector3.right * 100f;
            var first = new Squadron(Vector3.zero);
            var second = new Squadron(Vector3.zero);
            var third = new Squadron(Vector3.zero);
            assignments.AddTarget(site);
            assignments.RateTargets((_, defenders) => AddWings(defenders, 3));

            assignments.Assign(new[] { first, second }, AddWing);
            Assert.That(assignments.TryGetTarget(first, out _), Is.False,
                "Two wings against three equal defenders stay away.");
            Assert.That(assignments.TryGetTarget(second, out _), Is.False);

            assignments.Assign(new[] { first, second, third }, AddWing);
            Assert.That(assignments.TryGetTarget(first, out var target), Is.True);
            Assert.That(target, Is.EqualTo(site));
            Assert.That(assignments.TryGetTarget(second, out _), Is.True);
            Assert.That(assignments.TryGetTarget(third, out _), Is.True);
        }

        [Test]
        public void Assign_NoTargetsLeavesSquadronsUnassigned()
        {
            var assignments = new EnemySquadronCaptureAssignments();
            var squadron = new Squadron(Vector3.zero);
            assignments.Assign(new[] { squadron });
            Assert.That(assignments.TryGetTarget(squadron, out _), Is.False);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void Assign_OpeningWingsFillNeighbourThenCentreThenOuter(int count)
        {
            var assignments = new EnemySquadronCaptureAssignments();
            Vector3[] lanes = { Vector3.right * 100f, Vector3.forward * 200f, Vector3.left * 300f };
            assignments.AddTarget(Vector3.right);
            for (int i = lanes.Length - 1; i >= 0; i--) assignments.AddTarget(lanes[i]);
            foreach (Vector3 lane in lanes) assignments.PrioritizeArea(lane, 20f);
            var wings = new Squadron[count];
            for (int i = 0; i < wings.Length; i++) wings[i] = new Squadron(Vector3.zero);

            assignments.Assign(wings);

            for (int i = 0; i < wings.Length; i++)
            {
                Assert.That(assignments.TryGetTarget(wings[i], out Vector3 target), Is.True);
                Assert.That(target, Is.EqualTo(lanes[i]));
            }
        }

        [Test]
        public void Assign_ContestedOpeningZoneIsLeftAloneUntilTheWingsMatchItsDefenders()
        {
            var assignments = new EnemySquadronCaptureAssignments();
            Vector3 contested = Vector3.right * 100f;
            Vector3 outer = Vector3.left * 100f;
            assignments.AddTarget(contested);
            assignments.AddTarget(outer);
            assignments.RateTargets((target, defenders) => { if (target == contested) AddWings(defenders, 3); });
            assignments.PrioritizeArea(contested, 20f);
            assignments.PrioritizeArea(outer, 20f);
            var wings = new[]
            {
                new Squadron(Vector3.right * 90f), new Squadron(Vector3.right * 80f),
                new Squadron(Vector3.right * 70f), new Squadron(Vector3.right * 60f)
            };

            assignments.Assign(new[] { wings[0], wings[1] }, AddWing);

            Assert.That(assignments.TryGetTarget(wings[0], out Vector3 firstTarget), Is.True);
            Assert.That(assignments.TryGetTarget(wings[1], out Vector3 secondTarget), Is.True);
            Assert.That(new[] { firstTarget, secondTarget }, Is.EqualTo(new[] { outer, outer }),
                "Two wings cannot win the contested zone, so neither flies into it.");

            assignments.Assign(wings, AddWing);

            int atContested = 0;
            foreach (Squadron wing in wings)
                if (assignments.TryGetTarget(wing, out Vector3 target) && target == contested) atContested++;
            Assert.That(atContested, Is.EqualTo(3), "Three wings match the three defenders and take the zone together.");
        }

        [Test]
        public void Assign_AfterTheRelayIsHeld_CapturesTheClosestSatellitePadBeforeMines()
        {
            var assignments = new EnemySquadronCaptureAssignments();
            var squadron = new Squadron(Vector3.zero);
            Vector3 centre = Vector3.right * 100f;
            Vector3 nearestPad = centre + Vector3.right * 10f;
            assignments.AddTarget(centre + Vector3.up);
            assignments.AddTarget(centre + Vector3.forward * 15f, isSatellitePad: true);
            assignments.AddTarget(nearestPad, isSatellitePad: true);
            assignments.PrioritizeArea(centre, 20f);

            assignments.Assign(new[] { squadron });

            Assert.That(assignments.TryGetTarget(squadron, out Vector3 target), Is.True);
            Assert.That(target, Is.EqualTo(nearestPad));
        }

        [Test]
        public void AssignInZone_ContestedPadIsSkippedForTheNextFreeSiteInTheSameZone()
        {
            var assignments = new EnemySquadronCaptureAssignments();
            var wing = new Squadron(Vector3.zero);
            Vector3 contested = new Vector3(-50f, 0f, 0f);
            Vector3 free = new Vector3(50f, 0f, 0f);
            Vector3 outside = new Vector3(-900f, 0f, 0f);
            assignments.AddTarget(contested, isSatellitePad: true);
            assignments.AddTarget(free, isSatellitePad: true);
            assignments.AddTarget(outside, isSatellitePad: true);
            assignments.RateTargets((target, defenders) => { if (target == contested) AddWings(defenders, 3); });
            Vector3 home = new Vector3(-1000f, 0f, 0f);

            assignments.BeginAssignment();
            assignments.AssignInZone(new[] { wing }, Vector3.zero, 200f, home, false, new Vector3(-100f, 0f, 0f));

            Assert.That(assignments.TryGetTarget(wing, out Vector3 target), Is.True);
            Assert.That(target, Is.EqualTo(free), "The wing stays in its zone and takes the free pad.");
        }

        [Test]
        public void AssignInZone_NoFreeSiteBaitsAtTheZoneEdgeAndReturnsOnceASiteIsFree()
        {
            var assignments = new EnemySquadronCaptureAssignments();
            var wing = new Squadron(Vector3.zero);
            Vector3 pad = new Vector3(50f, 0f, 0f);
            Vector3 bait = new Vector3(-100f, 0f, 0f);
            assignments.AddTarget(pad, isSatellitePad: true);
            assignments.RateTargets((target, defenders) => AddWings(defenders, 3));

            assignments.BeginAssignment();
            assignments.AssignInZone(new[] { wing }, Vector3.zero, 200f, Vector3.left * 1000f, false, bait);
            assignments.TryGetTarget(wing, out Vector3 baiting);

            assignments.ClearTargets();
            assignments.AddTarget(pad, isSatellitePad: true);
            assignments.RateTargets((target, defenders) => { });
            assignments.BeginAssignment();
            assignments.AssignInZone(new[] { wing }, Vector3.zero, 200f, Vector3.left * 1000f, false, bait);
            assignments.TryGetTarget(wing, out Vector3 returned);

            Assert.That(baiting, Is.EqualTo(bait));
            Assert.That(assignments.IsSite(bait), Is.False, "A bait point is not a capture site.");
            Assert.That(returned, Is.EqualTo(pad));
        }

        [Test]
        public void AssignInZone_PadsFirstBaseSideFirstThenTheGroupOrder()
        {
            var assignments = new EnemySquadronCaptureAssignments();
            Vector3 relay = Vector3.zero;
            Vector3 mine = new Vector3(0f, 0f, 80f);
            Vector3 farPad = new Vector3(120f, 0f, 0f);
            Vector3 nearPad = new Vector3(-120f, 0f, 0f);
            assignments.AddTarget(relay);
            assignments.AddTarget(mine, isMine: true);
            assignments.AddTarget(farPad, isSatellitePad: true);
            assignments.AddTarget(nearPad, isSatellitePad: true);
            assignments.RateTargets((target, defenders) => { });
            Vector3 home = Vector3.left * 1000f;
            var wings = new ISquadron[4];
            for (int i = 0; i < wings.Length; i++) wings[i] = new Squadron(Vector3.zero);

            assignments.BeginAssignment();
            assignments.AssignInZone(wings, relay, 200f, home, relayBeforeMines: true, home);

            Vector3[] targets = new Vector3[wings.Length];
            for (int i = 0; i < wings.Length; i++) assignments.TryGetTarget(wings[i], out targets[i]);
            Assert.That(targets, Is.EqualTo(new[] { nearPad, farPad, relay, mine }));
        }

        private static void AddWing(ISquadron squadron, ForceComposition force) => AddWings(force, 1);

        // Equal units on both sides: N wings against M defenders is an N / M advantage.
        private static void AddWings(ForceComposition force, int count)
        {
            float[] rates = new float[ForceComposition.ClassSlots];
            for (int i = 0; i < rates.Length; i++) rates[i] = 10f;
            var profile = new UnitCombatProfile(ShipClass.Fighter, 100f, 0f, rates, rates,
                new float[ForceComposition.ClassSlots], Array.Empty<UnitCombatProfile>());
            for (int i = 0; i < count; i++) force.AddNew(profile, false);
        }

        private sealed class Squadron : ISquadron
        {
            public event Action Released { add { } remove { } }
            public long EntityId => 0;
            public Vector3 WorldPosition { get; }
            public Squadron(Vector3 position) => WorldPosition = position;
            public void Guard(IEntity friendly, Vector3 offset) { }
            public void AttackMoveTo(Vector3 worldPosition) { }
            public void Hunt() { }
        }
    }
}
