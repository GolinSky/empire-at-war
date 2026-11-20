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
        public void Pace_ShipDamagedBeforeTheOrderDoesNotSetTheStragglerFloor()
        {
            PaceCluster pace = new PaceCluster(orderId: 1);
            pace.Join(FAST, referenceSpeed: FAST);
            pace.Join(FAST, referenceSpeed: FAST);
            float damaged = FAST * PaceClusterTuning.Default.StragglerSpeedRatio * 0.5f;
            pace.Join(damaged, referenceSpeed: FAST);

            Assert.That(pace.IsBelowStragglerSpeed(damaged), Is.True,
                "Engine damage at issue must not lower the floor to the damaged ship's speed.");
            Assert.That(pace.Pace, Is.EqualTo(FAST));
        }

        [Test]
        public void TryGetSpeedCap_SlowsLeadersFreesLaggersAndDetachesLongStragglers()
        {
            PaceCluster pace = new PaceCluster(orderId: 1);
            PaceClusterTuning tuning = pace.Tuning;
            pace.Join(SLOW, remaining: 1000f);
            pace.Join(FAST, remaining: 1000f);
            float timer = 0f;
            float mean = pace.MeanRemaining;

            Assert.That(pace.TryGetSpeedCap(mean, 0.1f, ref timer, out float cap), Is.True);
            Assert.That(cap, Is.EqualTo(pace.Pace));

            Assert.That(pace.TryGetSpeedCap(mean - tuning.LeadTolerance * 2f, 0.1f, ref timer, out cap), Is.True);
            Assert.That(cap, Is.LessThan(pace.Pace), "A member ahead of the mean slows down.");

            Assert.That(pace.TryGetSpeedCap(mean + tuning.LeadTolerance * 2f, 0.1f, ref timer, out cap), Is.True);
            Assert.That(cap, Is.EqualTo(float.PositiveInfinity), "A member behind the mean catches up uncapped.");

            float straggling = mean + tuning.StragglerDistance * 2f + 3f * pace.Radius;
            Assert.That(pace.TryGetSpeedCap(straggling, tuning.StragglerTime * 0.5f, ref timer, out _), Is.True);
            Assert.That(pace.TryGetSpeedCap(straggling, tuning.StragglerTime, ref timer, out _), Is.False,
                "A member trailing past the straggler distance for the straggler time detaches.");
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

        [Test]
        public void Escorts_FollowTheNearestShipAndDoNotSetPaceOrProgressWhileAShipRemains()
        {
            PaceCluster pace = new PaceCluster();
            int near = pace.Join(FAST, 100f, new Vector2(0f, 0f), 20f, Vector2.UnitX);
            int far = pace.Join(SLOW, 300f, new Vector2(500f, 0f), 25f, Vector2.UnitY);
            int escort = pace.Join(SLOW * 0.5f, 900f, new Vector2(50f, 0f), 5f, Vector2.UnitX, isEscort: true);

            Assert.That(pace.Pace, Is.EqualTo(SLOW), "Only ships set the pace.");
            Assert.That(pace.MeanRemaining, Is.EqualTo(200f), "Escorts do not hold the ships back.");
            Assert.That(pace.TryGetEscortAnchor(new Vector2(50f, 0f), out Vector2 anchor, out Vector2 heading,
                out float radius), Is.True);
            Assert.That(anchor, Is.EqualTo(new Vector2(0f, 0f)));
            Assert.That(heading, Is.EqualTo(Vector2.UnitX));
            Assert.That(radius, Is.EqualTo(20f));

            pace.Leave(near);
            pace.TryGetEscortAnchor(new Vector2(50f, 0f), out anchor, out _, out _);
            Assert.That(anchor, Is.EqualTo(new Vector2(500f, 0f)));

            pace.Leave(far);
            Assert.That(pace.TryGetEscortAnchor(new Vector2(50f, 0f), out _, out _, out _), Is.False);
            pace.Leave(escort);
        }

        [Test]
        public void Escorts_WithoutShipsPaceTheirSlowestEscort()
        {
            PaceCluster pace = new PaceCluster();
            pace.Join(FAST, 100f, isEscort: true);
            pace.Join(SLOW, 300f, isEscort: true);

            Assert.That(pace.Pace, Is.EqualTo(SLOW));
            Assert.That(pace.MeanRemaining, Is.EqualTo(200f));
            Assert.That(pace.TryGetEscortAnchor(Vector2.Zero, out _, out _, out _), Is.False);
        }

        [Test]
        public void MergeInto_KeepsEscortMembership()
        {
            PaceCluster source = new PaceCluster();
            source.Join(FAST, 100f, new Vector2(10f, 0f), 5f, isEscort: true);
            source.Join(FAST, 100f, new Vector2(20f, 0f), 5f, isEscort: true);
            PaceCluster target = new PaceCluster();
            target.Join(SLOW, 100f, new Vector2(0f, 0f), 20f);
            target.Join(SLOW, 100f, new Vector2(40f, 0f), 20f);

            source.MergeInto(target);

            Assert.That(target.Pace, Is.EqualTo(SLOW));
            Assert.That(source.TryGetEscortAnchor(new Vector2(10f, 0f), out Vector2 anchor, out _, out _), Is.True);
            Assert.That(anchor, Is.EqualTo(new Vector2(0f, 0f)));
        }
    }
}
