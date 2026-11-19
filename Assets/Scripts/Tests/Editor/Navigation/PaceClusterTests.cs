using System.Numerics;
using EmpireAtWar.Entities.BaseEntity.Orders;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Navigation")]
    public sealed class PaceClusterTests
    {
        private const float FAST = 30f;
        private const float SLOW = 20f;

        [Test]
        public void Pace_IsSlowestMemberAndRisesWhenItLeaves()
        {
            PaceCluster pace = new PaceCluster();
            pace.Join(FAST);
            int slow = pace.Join(SLOW);
            pace.Join(FAST);

            Assert.That(pace.Pace, Is.EqualTo(SLOW));
            pace.Leave(slow);
            Assert.That(pace.Pace, Is.EqualTo(FAST));
        }

        [Test]
        public void Pace_FollowsReportedSpeedChangesAndIgnoresUnboundedMembers()
        {
            PaceCluster pace = new PaceCluster();
            int first = pace.Join(FAST);
            pace.Join(FAST);

            pace.Report(first, SLOW);
            Assert.That(pace.Pace, Is.EqualTo(SLOW));
            pace.Report(first, float.PositiveInfinity);
            Assert.That(pace.Pace, Is.EqualTo(FAST), "A member that cannot move must not hold the cluster back.");
        }

        [Test]
        public void Pace_ExcludesMembersBelowStragglerSpeedFloor()
        {
            PaceCluster pace = new PaceCluster(orderId: 1, issuePace: FAST);
            int damaged = pace.Join(FAST);
            pace.Join(FAST);
            pace.Join(FAST);

            float belowFloor = FAST * PaceClusterTuning.Default.StragglerSpeedRatio * 0.5f;
            Assert.That(pace.IsBelowStragglerSpeed(belowFloor), Is.True);

            pace.Report(damaged, belowFloor);
            Assert.That(pace.Pace, Is.EqualTo(FAST));
        }

        [Test]
        public void Pace_IsUncappedWithFewerThanTwoMembers()
        {
            PaceCluster pace = new PaceCluster();
            Assert.That(pace.Pace, Is.EqualTo(float.PositiveInfinity));

            int first = pace.Join(SLOW);
            Assert.That(pace.Pace, Is.EqualTo(float.PositiveInfinity));

            int second = pace.Join(FAST);
            Assert.That(pace.Pace, Is.EqualTo(SLOW));
            pace.Leave(second);
            Assert.That(pace.Pace, Is.EqualTo(float.PositiveInfinity));
            pace.Leave(first);
            Assert.That(pace.Pace, Is.EqualTo(float.PositiveInfinity));
        }

        [Test]
        public void MeanRemaining_ReflectsActiveMembersAndUpdatesOnReportOrLeave()
        {
            PaceCluster pace = new PaceCluster();
            int a = pace.Join(FAST, remaining: 400f);
            int b = pace.Join(FAST, remaining: 600f);

            Assert.That(pace.MeanRemaining, Is.EqualTo(500f).Within(0.01f));

            pace.Report(a, FAST, remaining: 200f);
            Assert.That(pace.MeanRemaining, Is.EqualTo(400f).Within(0.01f));

            pace.Leave(b);
            Assert.That(pace.MeanRemaining, Is.EqualTo(200f).Within(0.01f));
        }

        [Test]
        public void MergeInto_ForwardsReportsLeavesAndPaceWithoutMemberCallbacks()
        {
            PaceCluster source = new PaceCluster(orderId: 7, issuePace: FAST);
            PaceCluster target = new PaceCluster(orderId: 7, issuePace: FAST);

            int sourceMember = source.Join(FAST, remaining: 300f, position: new Vector2(0f, 0f), radius: 20f,
                heading: Vector2.UnitX);
            int targetMember = target.Join(SLOW, remaining: 500f, position: new Vector2(50f, 0f), radius: 20f,
                heading: Vector2.UnitX);

            source.MergeInto(target);

            Assert.That(source.IsMerged, Is.True);
            Assert.That(source.Pace, Is.EqualTo(SLOW));
            Assert.That(target.MemberCount, Is.EqualTo(2));
            Assert.That(source.MeanRemaining, Is.EqualTo(400f).Within(0.01f));

            source.Report(sourceMember, FAST, remaining: 100f, position: new Vector2(10f, 0f), heading: Vector2.UnitX);
            Assert.That(target.MeanRemaining, Is.EqualTo(300f).Within(0.01f));

            source.Leave(sourceMember);
            Assert.That(target.MemberCount, Is.EqualTo(1));
            Assert.That(target.Pace, Is.EqualTo(float.PositiveInfinity));

            target.Leave(targetMember);
            Assert.That(target.MemberCount, Is.EqualTo(0));
        }
    }
}
