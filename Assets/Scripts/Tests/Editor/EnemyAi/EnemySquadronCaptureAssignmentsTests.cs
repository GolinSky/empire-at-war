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
        public void Assign_NoTargetsLeavesSquadronsUnassigned()
        {
            var assignments = new EnemySquadronCaptureAssignments();
            var squadron = new Squadron(Vector3.zero);
            assignments.Assign(new[] { squadron });
            Assert.That(assignments.TryGetTarget(squadron, out _), Is.False);
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
