using System;
using EmpireAtWar.Entities.BaseEntity;
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
            assignments.AddTarget(site, requiredWings: 3);

            assignments.Assign(new[] { first, second });
            Assert.That(assignments.TryGetTarget(first, out _), Is.False);
            Assert.That(assignments.TryGetTarget(second, out _), Is.False);

            assignments.Assign(new[] { first, second, third });
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
        public void Assign_ContestedOpeningZoneGetsOneWingUntilEveryZoneHasOne()
        {
            var assignments = new EnemySquadronCaptureAssignments();
            Vector3 contested = Vector3.right * 100f;
            Vector3 outer = Vector3.left * 100f;
            assignments.AddTarget(contested, requiredWings: 3);
            assignments.AddTarget(outer);
            assignments.PrioritizeArea(contested, 20f);
            assignments.PrioritizeArea(outer, 20f);
            var first = new Squadron(Vector3.right * 90f);
            var second = new Squadron(Vector3.right * 80f);

            assignments.Assign(new[] { first, second });

            Assert.That(assignments.TryGetTarget(first, out Vector3 firstTarget), Is.True);
            Assert.That(assignments.TryGetTarget(second, out Vector3 secondTarget), Is.True);
            Assert.That(new[] { firstTarget, secondTarget }, Is.EquivalentTo(new[] { contested, outer }));
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
