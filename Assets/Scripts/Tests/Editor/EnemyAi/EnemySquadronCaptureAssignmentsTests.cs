using System;
using System.Collections.Generic;
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

        // Capture radii from the capture check in the plan: mine 60, satellite pad 500.
        private const float MINE_RADIUS = 60f;
        private const float PAD_RADIUS = 500f;

        [Test]
        public void AssignInZone_GroupLandsOnTheBaseSideMiningSpotAndHoldsInsideItsCircle()
        {
            var assignments = new EnemySquadronCaptureAssignments();
            Vector3 home = new Vector3(-2000f, 0f, 0f);
            Vector3 nearSpot = new Vector3(-270f, 0f, 0f);
            Vector3 farSpot = new Vector3(270f, 0f, 0f);
            assignments.AddMiningSpot(nearSpot, MINE_RADIUS);
            assignments.AddMiningSpot(farSpot, MINE_RADIUS);
            assignments.AddTarget(nearSpot, isMine: true);
            assignments.AddTarget(farSpot, isMine: true);
            assignments.AddTarget(new Vector3(-470f, 0f, 0f), isSatellitePad: true, captureRadius: PAD_RADIUS);
            assignments.RateTargets((target, defenders) => { });
            var wings = new ISquadron[] { new Squadron(home), new Squadron(home) };

            assignments.BeginAssignment();
            assignments.AssignInZone(wings, Vector3.zero, 1000f, home, AddWing);

            foreach (ISquadron wing in wings)
            {
                Assert.That(assignments.TryGetTarget(wing, out Vector3 target), Is.True);
                Assert.That(target, Is.EqualTo(nearSpot), "The whole group lands on the base-side mining spot.");
                Assert.That(assignments.TryGetHold(wing, out float radius), Is.True);
                Assert.That(radius, Is.EqualTo(MINE_RADIUS), "An uncontested spot is held from inside its circle.");
            }
        }

        [Test]
        public void AssignInZone_OwnedMineSpotStillTakesThePadsInItsReach()
        {
            var assignments = new EnemySquadronCaptureAssignments();
            Vector3 home = new Vector3(-2000f, 0f, 0f);
            Vector3 spentSpot = new Vector3(-600f, 0f, -600f);
            Vector3 padSpot = new Vector3(-270f, 0f, 0f);
            Vector3 farSpot = new Vector3(270f, 0f, 0f);
            assignments.AddMiningSpot(spentSpot, MINE_RADIUS);
            assignments.AddMiningSpot(padSpot, MINE_RADIUS);
            assignments.AddMiningSpot(farSpot, MINE_RADIUS);
            assignments.AddTarget(farSpot, isMine: true);
            // The mine at padSpot is already owned (no target), but a free pad still lies in reach of it.
            assignments.AddTarget(new Vector3(-270f, 0f, 400f), isSatellitePad: true, captureRadius: PAD_RADIUS);
            assignments.RateTargets((target, defenders) => { });
            var wing = new Squadron(home);

            assignments.BeginAssignment();
            assignments.AssignInZone(new ISquadron[] { wing }, Vector3.zero, 1000f, home, AddWing);

            Assert.That(assignments.TryGetTarget(wing, out Vector3 target), Is.True);
            Assert.That(target, Is.EqualTo(padSpot),
                "A spot with nothing left is skipped; an owned mine spot with a free pad in reach is not.");
        }

        [Test]
        public void AssignInZone_ContestedSpotIsFoughtForOnlyWhenTheGroupMatchesItsDefenders()
        {
            var assignments = new EnemySquadronCaptureAssignments();
            Vector3 home = new Vector3(-2000f, 0f, 0f);
            Vector3 contested = new Vector3(-270f, 0f, 0f);
            Vector3 free = new Vector3(270f, 0f, 0f);
            assignments.AddMiningSpot(contested, MINE_RADIUS);
            assignments.AddMiningSpot(free, MINE_RADIUS);
            assignments.AddTarget(contested, isMine: true);
            assignments.AddTarget(free, isMine: true);
            assignments.RateTargets((target, defenders) => { if (target == contested) AddWings(defenders, 2); });
            var lone = new Squadron(home);
            var group = new ISquadron[] { new Squadron(home), new Squadron(home), new Squadron(home) };

            assignments.BeginAssignment();
            assignments.AssignInZone(new ISquadron[] { lone }, Vector3.zero, 1000f, home, AddWing);
            assignments.TryGetTarget(lone, out Vector3 loneTarget);
            assignments.BeginAssignment();
            assignments.AssignInZone(group, Vector3.zero, 1000f, home, AddWing);

            Assert.That(loneTarget, Is.EqualTo(free), "One wing cannot beat two defenders: it takes the next spot.");
            foreach (ISquadron wing in group)
            {
                Assert.That(assignments.TryGetTarget(wing, out Vector3 target), Is.True);
                Assert.That(target, Is.EqualTo(contested), "Three wings beat two defenders and fight for the spot.");
                Assert.That(assignments.TryGetHold(wing, out _), Is.False, "A contested spot is attack-moved onto.");
            }
        }

        [Test]
        public void AssignInZone_LargeGroupSplitsInHalfOverTheTwoBestSpots()
        {
            var assignments = new EnemySquadronCaptureAssignments();
            Vector3 home = new Vector3(-2000f, 0f, 0f);
            Vector3 nearSpot = new Vector3(-270f, 0f, 0f);
            Vector3 farSpot = new Vector3(270f, 0f, 0f);
            assignments.AddMiningSpot(nearSpot, MINE_RADIUS);
            assignments.AddMiningSpot(farSpot, MINE_RADIUS);
            assignments.AddTarget(nearSpot, isMine: true);
            assignments.AddTarget(farSpot, isMine: true);
            assignments.RateTargets((target, defenders) => { });
            var wings = new ISquadron[EnemySquadronCaptureAssignments.SPLIT_GROUP_WINGS];
            for (int i = 0; i < wings.Length; i++) wings[i] = new Squadron(home);

            assignments.BeginAssignment();
            assignments.AssignInZone(wings, Vector3.zero, 1000f, home, AddWing);

            int atNear = 0;
            int atFar = 0;
            foreach (ISquadron wing in wings)
            {
                assignments.TryGetTarget(wing, out Vector3 target);
                if (target == nearSpot) atNear++;
                else if (target == farSpot) atFar++;
            }
            Assert.That(atNear, Is.EqualTo(wings.Length / 2));
            Assert.That(atFar, Is.EqualTo(wings.Length - wings.Length / 2));
        }

        [Test]
        public void AssignInZone_NoWinnableSpot_TakesTheRemainingSiteNearestHomeAndIsNeverIdle()
        {
            var assignments = new EnemySquadronCaptureAssignments();
            Vector3 home = new Vector3(-2000f, 0f, 0f);
            Vector3 spot = new Vector3(0f, 0f, 270f);
            Vector3 freePad = new Vector3(470f, 0f, -470f);
            Vector3 nearPad = new Vector3(-470f, 0f, -470f);
            assignments.AddMiningSpot(spot, MINE_RADIUS);
            assignments.AddTarget(spot, isMine: true);
            assignments.AddTarget(freePad, isSatellitePad: true, captureRadius: PAD_RADIUS);
            assignments.AddTarget(nearPad, isSatellitePad: true, captureRadius: PAD_RADIUS);
            bool padContested = false;
            Action<Vector3, ForceComposition> rate = (target, defenders) =>
            {
                if (target == spot || target == nearPad || padContested && target == freePad) AddWings(defenders, 3);
            };
            var wing = new Squadron(home);

            assignments.RateTargets(rate);
            assignments.BeginAssignment();
            assignments.AssignInZone(new ISquadron[] { wing }, Vector3.zero, 1000f, home, AddWing);
            assignments.TryGetTarget(wing, out Vector3 freeTarget);

            padContested = true;
            assignments.RateTargets(rate);
            assignments.BeginAssignment();
            assignments.AssignInZone(new ISquadron[] { wing }, Vector3.zero, 1000f, home, AddWing);
            assignments.TryGetTarget(wing, out Vector3 foughtTarget);

            Assert.That(freeTarget, Is.EqualTo(freePad), "With no winnable spot the wing takes the free pad.");
            Assert.That(foughtTarget, Is.EqualTo(nearPad),
                "With every site contested the wing fights for the one nearest home instead of idling.");
        }

        // Round 5, E3a: group A spreads at least two wings to every mining spot with work it can win.
        [Test]
        public void AssignInZone_SpreadingGroupSendsAtLeastTwoWingsToEachSpot()
        {
            var assignments = new EnemySquadronCaptureAssignments();
            Vector3 home = new Vector3(-2000f, 0f, 0f);
            Vector3[] spots = { new Vector3(-270f, 0f, 0f), new Vector3(270f, 0f, 0f), new Vector3(0f, 0f, 400f) };
            foreach (Vector3 spot in spots)
            {
                assignments.AddMiningSpot(spot, MINE_RADIUS);
                assignments.AddTarget(spot, isMine: true);
            }
            // The base-side spot is held by one defender: a two-wing part wins it.
            assignments.RateTargets((target, defenders) => { if (target == spots[0]) AddWings(defenders, 1); });
            var wings = new ISquadron[5];
            for (int i = 0; i < wings.Length; i++) wings[i] = new Squadron(home);

            assignments.BeginAssignment();
            assignments.AssignInZone(wings, Vector3.zero, 1000f, home, AddWing, spread: true);

            var counts = new Dictionary<Vector3, int>();
            foreach (ISquadron wing in wings)
            {
                Assert.That(assignments.TryGetTarget(wing, out Vector3 target), Is.True, "No wing is left idle.");
                counts[target] = counts.TryGetValue(target, out int count) ? count + 1 : 1;
            }
            Assert.That(counts.Count, Is.EqualTo(wings.Length / EnemySquadronCaptureAssignments.MIN_SPOT_WINGS));
            foreach (KeyValuePair<Vector3, int> spot in counts)
                Assert.That(spot.Value, Is.GreaterThanOrEqualTo(EnemySquadronCaptureAssignments.MIN_SPOT_WINGS));
            Assert.That(counts.ContainsKey(spots[0]), Is.True, "Two wings beat the lone defender.");
        }

        [Test]
        public void AssignInZone_SpreadingGroupNeverParksOnAContestedSiteItCannotWin()
        {
            var assignments = new EnemySquadronCaptureAssignments();
            Vector3 home = new Vector3(-2000f, 0f, 0f);
            Vector3 spot = new Vector3(0f, 0f, 270f);
            Vector3 pad = new Vector3(-470f, 0f, -470f);
            assignments.AddMiningSpot(spot, MINE_RADIUS);
            assignments.AddTarget(spot, isMine: true);
            assignments.AddTarget(pad, isSatellitePad: true, captureRadius: PAD_RADIUS);
            assignments.RateTargets((target, defenders) => AddWings(defenders, 5));
            var wings = new ISquadron[] { new Squadron(home), new Squadron(home) };

            assignments.BeginAssignment();
            assignments.AssignInZone(wings, Vector3.zero, 1000f, home, AddWing, spread: true);

            foreach (ISquadron wing in wings)
            {
                Assert.That(assignments.TryGetTarget(wing, out Vector3 target), Is.True);
                Assert.That(target, Is.EqualTo(Vector3.zero), "Group A holds the zone centre instead.");
            }
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

        // W22: pads inside each other's capture circle are one wing target, so more pads never ask for more wings.
        [TestCase(2)]
        [TestCase(4)]
        public void OverlappingPads_AreOneTargetWhateverTheirCount(int pads)
        {
            var assignments = new EnemySquadronCaptureAssignments();
            for (int i = 0; i < pads; i++)
            {
                float angle = i * Mathf.PI * 2f / pads;
                assignments.AddTarget(new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 200f,
                    isSatellitePad: true, captureRadius: PAD_RADIUS);
            }
            var wings = new[] { new Squadron(Vector3.zero), new Squadron(Vector3.zero), new Squadron(Vector3.zero) };

            assignments.Assign(wings);

            var targets = new HashSet<Vector3>();
            foreach (Squadron wing in wings)
                if (assignments.TryGetTarget(wing, out Vector3 target)) targets.Add(target);
            Assert.That(targets.Count, Is.EqualTo(1));
        }

        [Test]
        public void PadsBeyondEachOthersCaptureCircle_StaySeparateTargets()
        {
            var assignments = new EnemySquadronCaptureAssignments();
            assignments.AddTarget(Vector3.right * 470f, isSatellitePad: true, captureRadius: PAD_RADIUS);
            assignments.AddTarget(Vector3.left * 470f, isSatellitePad: true, captureRadius: PAD_RADIUS);
            var wings = new[] { new Squadron(Vector3.zero), new Squadron(Vector3.zero) };

            assignments.Assign(wings);

            assignments.TryGetTarget(wings[0], out Vector3 first);
            assignments.TryGetTarget(wings[1], out Vector3 second);
            Assert.That(first, Is.Not.EqualTo(second));
        }

        private sealed class Squadron : ISquadron
        {
            public event Action Released { add { } remove { } }
            public long EntityId => 0;
            public Vector3 WorldPosition { get; }
            public bool IsEngaging => false;
            public float IdleLeashRadius => 0f;
            public Squadron(Vector3 position) => WorldPosition = position;
            public void Guard(IEntity friendly, Vector3 offset) { }
            public void AttackMoveTo(Vector3 worldPosition) { }
            public void Hunt() { }
        }
    }
}
