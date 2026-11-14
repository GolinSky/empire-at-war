using System.Collections.Generic;
using System.Threading;
using EmpireAtWar.Components.Squadrons.Icon;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Squadrons")]
    public sealed class SquadronIconGroupingTests
    {
        [Test]
        public void NineSquadronsFillAThreeByThreeGridAndKeepTheirCellsThroughMovement()
        {
            using var grouping = new SquadronIconGrouping();
            var members = new Sample[9];
            for (int i = 0; i < members.Length; i++)
                members[i] = new Sample(i + 1, i);

            Update(grouping, members, 3);
            var cells = new Vector2Int[members.Length];
            var uniqueCells = new HashSet<Vector2Int>();
            long group = ReadCell(grouping, 1, out _);
            for (int i = 0; i < members.Length; i++)
            {
                Assert.That(ReadCell(grouping, i + 1, out cells[i]), Is.EqualTo(group));
                Assert.That(cells[i].x, Is.InRange(-1, 1));
                Assert.That(cells[i].y, Is.InRange(-1, 1));
                uniqueCells.Add(cells[i]);
                members[i] = new Sample(i + 1, 30 + i, 56);
            }
            Assert.That(uniqueCells.Count, Is.EqualTo(9));

            Update(grouping, members, 3);
            for (int i = 0; i < members.Length; i++)
                members[i] = new Sample(i + 1, 60 + i);
            Update(grouping, members, 3);
            for (int i = 0; i < members.Length; i++)
            {
                Assert.That(ReadCell(grouping, i + 1, out Vector2Int cell), Is.EqualTo(group));
                Assert.That(cell, Is.EqualTo(cells[i]));
            }
        }

        [Test]
        public void SmallGroupsFillTheCenterRowBeforeOtherRows()
        {
            using var grouping = new SquadronIconGrouping();
            Update(grouping, new[] { new Sample(1, 0), new Sample(2, 10) }, 6);
            ReadCell(grouping, 1, out Vector2Int first);
            ReadCell(grouping, 2, out Vector2Int second);
            Assert.That(second.y, Is.EqualTo(first.y), "Two squadrons sit side by side.");

            Update(grouping, new[] { new Sample(1, 0), new Sample(2, 10), new Sample(3, 20) }, 6);
            ReadCell(grouping, 3, out Vector2Int third);
            Assert.That(third.y, Is.EqualTo(first.y), "Three squadrons form one row.");
        }

        [Test]
        public void PassingSquadronAndOtherOwnerCannotJoinIdleGrid()
        {
            using var grouping = new SquadronIconGrouping();
            Update(grouping, new[]
            {
                new Sample(1, 0),
                new Sample(2, 10),
                new Sample(3, 15, 70),
                new Sample(4, 5, owner: 1)
            }, 6);

            Assert.That(ReadCell(grouping, 1, out _), Is.EqualTo(ReadCell(grouping, 2, out _)));
            Assert.That(grouping.TryGetSlot(3, out _, out _), Is.False);
            Assert.That(grouping.TryGetSlot(4, out _, out _), Is.False);
        }

        [Test]
        public void DivergingMembersSplitAfterDwellWithTheirRelativeCells()
        {
            using var grouping = new SquadronIconGrouping();
            var members = new[] { new Sample(1, 0), new Sample(2, 2), new Sample(3, 4), new Sample(4, 6) };
            Update(grouping, members, 3);
            long originalGroup = ReadCell(grouping, 1, out Vector2Int firstCell);
            Assert.That(ReadCell(grouping, 2, out Vector2Int secondCell), Is.EqualTo(originalGroup));
            Assert.That(ReadCell(grouping, 3, out Vector2Int thirdCell), Is.EqualTo(originalGroup));
            Assert.That(ReadCell(grouping, 4, out Vector2Int fourthCell), Is.EqualTo(originalGroup));

            members[2] = new Sample(3, 6, 45);
            members[3] = new Sample(4, 8, 45);
            Update(grouping, members, 2);
            Assert.That(ReadCell(grouping, 3, out _), Is.EqualTo(originalGroup));
            Update(grouping, members, 2);

            long splitGroup = ReadCell(grouping, 3, out Vector2Int splitThird);
            Assert.That(ReadCell(grouping, 4, out Vector2Int splitFourth), Is.EqualTo(splitGroup));
            Assert.That(splitGroup, Is.Not.EqualTo(originalGroup));
            Assert.That(splitThird - splitFourth, Is.EqualTo(thirdCell - fourthCell));
            Assert.That(ReadCell(grouping, 1, out Vector2Int firstAfter), Is.EqualTo(originalGroup));
            Assert.That(ReadCell(grouping, 2, out Vector2Int secondAfter), Is.EqualTo(originalGroup));
            Assert.That(firstAfter, Is.EqualTo(firstCell));
            Assert.That(secondAfter, Is.EqualTo(secondCell));
        }

        [Test]
        public void MergeRetainsDominantGroupAndReusesItsFreeCell()
        {
            using var grouping = new SquadronIconGrouping();
            Update(grouping, new[]
            {
                new Sample(1, 0), new Sample(2, 2), new Sample(3, 4), new Sample(4, 6),
                new Sample(5, 400), new Sample(6, 402)
            }, 3);
            long dominant = ReadCell(grouping, 1, out Vector2Int retained);
            ReadCell(grouping, 2, out Vector2Int vacancy);
            Assert.That(ReadCell(grouping, 5, out _), Is.Not.EqualTo(dominant));
            grouping.Forget(2);

            Update(grouping, new[]
            {
                new Sample(1, 0), new Sample(3, 4), new Sample(4, 6),
                new Sample(5, 8), new Sample(6, 10)
            }, 3);

            Assert.That(ReadCell(grouping, 1, out Vector2Int after), Is.EqualTo(dominant));
            Assert.That(after, Is.EqualTo(retained));
            Assert.That(ReadCell(grouping, 5, out Vector2Int fifth), Is.EqualTo(dominant));
            Assert.That(ReadCell(grouping, 6, out Vector2Int sixth), Is.EqualTo(dominant));
            Assert.That(fifth == vacancy || sixth == vacancy, Is.True);
        }

        [Test]
        public void ForgottenEntityCannotRecoverItsSlotFromAnInFlightSnapshot()
        {
            using var grouping = new SquadronIconGrouping();
            var original = new[] { new Sample(1, 0), new Sample(2, 2), new Sample(3, 4) };
            Update(grouping, original, 3);
            ReadCell(grouping, 2, out _);

            Schedule(grouping, original);
            grouping.Forget(2);
            Assert.That(grouping.TryGetSlot(2, out _, out _), Is.False);
            Consume(grouping);
            Assert.That(grouping.TryGetSlot(2, out _, out _), Is.False);
            Update(grouping, new[] { new Sample(1, 0), new Sample(2, 500), new Sample(3, 4) }, 1);
            Assert.That(grouping.TryGetSlot(2, out _, out _), Is.False);
        }

        [Test]
        public void DenseCompatibleCellUsesOneRepresentativeWhileSettling()
        {
            const int MEMBER_COUNT = 512;
            using var grouping = new SquadronIconGrouping();
            var members = new Sample[MEMBER_COUNT];
            for (int i = 0; i < members.Length; i++)
                members[i] = new Sample(i + 1, 0);

            for (int i = 0; i < 3; i++)
            {
                Update(grouping, members, 1);
                Assert.That(grouping.LastCandidateCount, Is.EqualTo(MEMBER_COUNT - 1));
            }
            Assert.That(ReadCell(grouping, 1, out _), Is.EqualTo(ReadCell(grouping, MEMBER_COUNT, out _)));
        }

        [Test]
        public void SparseOuterCellsCompactOnlyAfterMembershipSettles()
        {
            using var grouping = new SquadronIconGrouping();
            var initial = new Sample[9];
            for (int i = 0; i < initial.Length; i++)
                initial[i] = new Sample(i + 1, 0);
            Update(grouping, initial, 3);
            long id = ReadCell(grouping, 9, out Vector2Int outerCell);
            var remaining = new[] { new Sample(1, 0), new Sample(9, 0) };

            Update(grouping, remaining, 10);
            Assert.That(ReadCell(grouping, 9, out Vector2Int before), Is.EqualTo(id));
            Assert.That(before, Is.EqualTo(outerCell));
            Update(grouping, remaining, 8);
            Assert.That(ReadCell(grouping, 9, out Vector2Int after), Is.EqualTo(id));
            Assert.That(after, Is.Not.EqualTo(outerCell));
        }

        private static long ReadCell(SquadronIconGrouping grouping, long id, out Vector2Int cell)
        {
            Assert.That(grouping.TryGetSlot(id, out long group, out cell), Is.True, $"Squadron {id}");
            return group;
        }

        private static void Update(SquadronIconGrouping grouping, Sample[] members, int ticks)
        {
            for (int i = 0; i < ticks; i++)
            {
                Schedule(grouping, members);
                Consume(grouping);
            }
        }

        private static void Schedule(SquadronIconGrouping grouping, Sample[] members)
        {
            grouping.BeginSnapshot(members.Length);
            foreach (Sample sample in members)
                grouping.Add(sample.Id, sample.Owner, new Vector2(sample.Position, 0f), new Vector2(sample.Speed, 0f));
            grouping.Schedule();
        }

        private static void Consume(SquadronIconGrouping grouping)
        {
            Assert.That(SpinWait.SpinUntil(grouping.TryComplete, 5000), Is.True, "Grouping job did not complete.");
        }

        private readonly struct Sample
        {
            public readonly long Id;
            public readonly int Position;
            public readonly int Speed;
            public readonly int Owner;

            public Sample(long id, int position, int speed = 0, int owner = 0)
            {
                Id = id;
                Position = position;
                Speed = speed;
                Owner = owner;
            }
        }
    }
}
