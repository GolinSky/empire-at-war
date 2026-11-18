using EmpireAtWar.Entities.BaseEntity.Orders;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Navigation")]
    public sealed class GroupPaceTests
    {
        private const float FAST = 20f;
        private const float SLOW = 10f;

        [Test]
        public void Pace_IsSlowestMemberAndRisesWhenItLeaves()
        {
            GroupPace pace = new GroupPace();
            pace.Join(FAST);
            int slow = pace.Join(SLOW);
            pace.Join(FAST);

            Assert.That(pace.Pace, Is.EqualTo(SLOW));
            pace.Leave(slow);
            Assert.That(pace.Pace, Is.EqualTo(FAST));
        }

        [Test]
        public void Pace_FollowsReportedSpeedChanges()
        {
            GroupPace pace = new GroupPace();
            int first = pace.Join(FAST);
            pace.Join(FAST);

            pace.Report(first, SLOW);
            Assert.That(pace.Pace, Is.EqualTo(SLOW));
            pace.Report(first, float.PositiveInfinity);
            Assert.That(pace.Pace, Is.EqualTo(FAST), "A member that cannot move must not hold the group back.");
        }

        [Test]
        public void Pace_IsUncappedWithFewerThanTwoMembers()
        {
            GroupPace pace = new GroupPace();
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
    }
}
