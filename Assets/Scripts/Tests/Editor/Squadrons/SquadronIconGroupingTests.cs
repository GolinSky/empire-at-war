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
        private const float TICK = 0.1f;
        private const float PITCH = 84f;
        private const float JOIN = 1.25f * PITCH;
        private const float LEAVE = 3f * PITCH;

        [Test]
        public void NearbySquadronsJoinOneClusterAfterDwellWithDistinctRanks()
        {
            using var grouping = new SquadronIconGrouping();
            var members = new[] { new Sample(1, 0), new Sample(2, 50) };
            Update(grouping, members, 1);
            Assert.That(Read(grouping, 1).Id, Is.Not.EqualTo(Read(grouping, 2).Id), "Dwell not served yet.");

            Update(grouping, members, 5);
            Entry first = Read(grouping, 1);
            Entry second = Read(grouping, 2);
            Assert.That(second.Id, Is.EqualTo(first.Id));
            Assert.That(first.Count, Is.EqualTo(2));
            Assert.That(new[] { first.Rank, second.Rank }, Is.EquivalentTo(new[] { 0, 1 }));
        }

        [Test]
        public void LoneSquadronIsAOneByOneCluster()
        {
            using var grouping = new SquadronIconGrouping();
            Update(grouping, new[] { new Sample(1, 0), new Sample(2, 1000) }, 6);
            Entry lone = Read(grouping, 2);
            Assert.That(lone.Count, Is.EqualTo(1));
            Assert.That(lone.Rank, Is.Zero);
            Assert.That(lone.Id, Is.Not.EqualTo(Read(grouping, 1).Id));
        }

        [Test]
        public void OtherOwnersNeverShareACluster()
        {
            using var grouping = new SquadronIconGrouping();
            Update(grouping, new[] { new Sample(1, 0), new Sample(2, 10), new Sample(3, 5, 1) }, 8);
            Assert.That(Read(grouping, 1).Id, Is.EqualTo(Read(grouping, 2).Id));
            Assert.That(Read(grouping, 3).Id, Is.Not.EqualTo(Read(grouping, 1).Id));
        }

        [Test]
        public void MemberLeavesOnlyAfterStayingAwayForTheLeaveDwell()
        {
            using var grouping = new SquadronIconGrouping();
            var members = new[] { new Sample(1, 0), new Sample(2, 40), new Sample(3, 80) };
            Update(grouping, members, 10);
            long original = Read(grouping, 1).Id;
            Assert.That(Read(grouping, 3).Id, Is.EqualTo(original));

            members[2] = new Sample(3, 1000);
            Update(grouping, members, 3);
            Assert.That(Read(grouping, 3).Id, Is.EqualTo(original), "Hysteresis: away for 0.3 s only.");
            Update(grouping, members, 3);
            Assert.That(Read(grouping, 3).Id, Is.Not.EqualTo(original));
            Assert.That(Read(grouping, 3).Count, Is.EqualTo(1));
            Assert.That(Read(grouping, 1).Count, Is.EqualTo(2));
        }

        [Test]
        public void MemberBetweenTheJoinAndLeaveRadiiStaysInItsCluster()
        {
            using var grouping = new SquadronIconGrouping();
            var members = new[] { new Sample(1, 0), new Sample(2, 40) };
            Update(grouping, members, 6);
            members[1] = new Sample(2, JOIN + 60f);
            Update(grouping, members, 20);
            Assert.That(Read(grouping, 2).Id, Is.EqualTo(Read(grouping, 1).Id));
            Assert.That(JOIN + 60f, Is.LessThan(LEAVE));
        }

        [Test]
        public void RanksCompactWhenAMiddleMemberDisappears()
        {
            using var grouping = new SquadronIconGrouping();
            Update(grouping, new[] { new Sample(1, 0), new Sample(2, 10), new Sample(3, 20) }, 10);
            long cluster = Read(grouping, 1).Id;
            int firstRank = Read(grouping, 1).Rank;
            int secondRank = Read(grouping, 2).Rank;
            int thirdRank = Read(grouping, 3).Rank;
            Assert.That(new[] { firstRank, secondRank, thirdRank }, Is.EquivalentTo(new[] { 0, 1, 2 }));

            grouping.Forget(2);
            Update(grouping, new[] { new Sample(1, 0), new Sample(3, 20) }, 1);
            Assert.That(Read(grouping, 1).Id, Is.EqualTo(cluster));
            Assert.That(Read(grouping, 1).Count, Is.EqualTo(2));
            Assert.That(Read(grouping, 1).Rank, Is.EqualTo(firstRank < secondRank ? firstRank : firstRank - 1));
            Assert.That(Read(grouping, 3).Rank, Is.EqualTo(thirdRank < secondRank ? thirdRank : thirdRank - 1));
        }

        [Test]
        public void MergeKeepsTheLargerClusterFirstAndTheIncomingOrder()
        {
            using var grouping = new SquadronIconGrouping();
            var members = new[]
            {
                new Sample(1, 0), new Sample(2, 10), new Sample(3, 20),
                new Sample(4, 600), new Sample(5, 610)
            };
            Update(grouping, members, 10);
            Assert.That(Read(grouping, 4).Id, Is.Not.EqualTo(Read(grouping, 1).Id));
            bool incomingOrdered = Read(grouping, 4).Rank < Read(grouping, 5).Rank;

            members[3] = new Sample(4, 120);
            members[4] = new Sample(5, 130);
            Update(grouping, members, 10);
            long cluster = Read(grouping, 1).Id;
            for (int id = 2; id <= 5; id++) Assert.That(Read(grouping, id).Id, Is.EqualTo(cluster));
            Assert.That(Read(grouping, 1).Count, Is.EqualTo(5));
            int largestOriginal = Mathf.Max(Read(grouping, 1).Rank, Read(grouping, 2).Rank, Read(grouping, 3).Rank);
            Assert.That(Read(grouping, 4).Rank, Is.GreaterThan(largestOriginal));
            Assert.That(Read(grouping, 5).Rank, Is.GreaterThan(largestOriginal));
            Assert.That(Read(grouping, 4).Rank < Read(grouping, 5).Rank, Is.EqualTo(incomingOrdered));
        }

        [Test]
        public void ScreenRadiiScaleWithZoom()
        {
            var members = new[] { new Sample(1, 0), new Sample(2, 150) };
            using (var normal = new SquadronIconGrouping())
            {
                Update(normal, members, 10);
                Assert.That(Read(normal, 1).Id, Is.Not.EqualTo(Read(normal, 2).Id));
            }

            using var zoomedOut = new SquadronIconGrouping();
            for (int i = 0; i < 10; i++)
            {
                Schedule(zoomedOut, members, 1.5f * JOIN, 1.5f * LEAVE, 1.5f * PITCH);
                Consume(zoomedOut);
            }

            Assert.That(Read(zoomedOut, 1).Id, Is.EqualTo(Read(zoomedOut, 2).Id));
        }

        [Test]
        public void ForgottenEntityCannotRecoverItsSlotFromAnInFlightSnapshot()
        {
            using var grouping = new SquadronIconGrouping();
            var original = new[] { new Sample(1, 0), new Sample(2, 20), new Sample(3, 40) };
            Update(grouping, original, 8);
            Read(grouping, 2);

            Schedule(grouping, original);
            grouping.Forget(2);
            Assert.That(grouping.TryGetCluster(2, out _, out _, out _), Is.False);
            Consume(grouping);
            Assert.That(grouping.TryGetCluster(2, out _, out _, out _), Is.False);
        }

        [Test]
        public void ManySquadronsAtOnePointSettleIntoOneClusterWithUniqueRanks()
        {
            const int MEMBER_COUNT = 300;
            using var grouping = new SquadronIconGrouping();
            var members = new Sample[MEMBER_COUNT];
            for (int i = 0; i < members.Length; i++) members[i] = new Sample(i + 1, 0);

            Update(grouping, members, 12);
            long cluster = Read(grouping, 1).Id;
            var ranks = new HashSet<int>();
            for (int i = 1; i <= MEMBER_COUNT; i++)
            {
                Entry read = Read(grouping, i);
                Assert.That(read.Id, Is.EqualTo(cluster));
                Assert.That(read.Count, Is.EqualTo(MEMBER_COUNT));
                Assert.That(ranks.Add(read.Rank), Is.True);
            }

            Assert.That(ranks, Has.Count.EqualTo(MEMBER_COUNT));
        }

        private static Entry Read(SquadronIconGrouping grouping, long id)
        {
            Assert.That(grouping.TryGetCluster(id, out long cluster, out int rank, out int count), Is.True,
                $"Squadron {id}");
            return new Entry(cluster, rank, count);
        }

        private static void Update(SquadronIconGrouping grouping, Sample[] members, int ticks)
        {
            for (int i = 0; i < ticks; i++)
            {
                Schedule(grouping, members);
                Consume(grouping);
            }
        }

        private static void Schedule(SquadronIconGrouping grouping, Sample[] members, float join = JOIN,
            float leave = LEAVE, float pitch = PITCH)
        {
            grouping.BeginSnapshot(members.Length);
            foreach (Sample sample in members)
                grouping.Add(sample.Id, sample.Owner, new Vector2(sample.X, 0f));
            grouping.Schedule(TICK, join, leave, pitch);
        }

        private static void Consume(SquadronIconGrouping grouping) =>
            Assert.That(SpinWait.SpinUntil(grouping.TryComplete, 5000), Is.True, "Grouping job did not complete.");

        private readonly struct Entry
        {
            public readonly long Id;
            public readonly int Rank;
            public readonly int Count;

            public Entry(long id, int rank, int count)
            {
                Id = id;
                Rank = rank;
                Count = count;
            }
        }

        private readonly struct Sample
        {
            public readonly long Id;
            public readonly float X;
            public readonly int Owner;

            public Sample(long id, float x, int owner = 0)
            {
                Id = id;
                X = x;
                Owner = owner;
            }
        }
    }
}
