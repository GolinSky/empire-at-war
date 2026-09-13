using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Mvc;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class EntityComponentLifecycleTests
    {
        [Test]
        public void Release_ReleasesEveryComponentOnceAcrossRepeatedCalls()
        {
            TrackingComponent first = new TrackingComponent();
            TrackingComponent second = new TrackingComponent();
            EntityComponentLifecycle lifecycle = new EntityComponentLifecycle(
                new IMonoComponent[] { first, second });

            Assert.That(lifecycle.Release(), Is.True);
            Assert.That(lifecycle.Release(), Is.False);
            Assert.That(first.ReleaseCount, Is.EqualTo(1));
            Assert.That(second.ReleaseCount, Is.EqualTo(1));
        }

        private sealed class TrackingComponent : IMonoComponent
        {
            public int ReleaseCount { get; private set; }

            public void Release()
            {
                ReleaseCount++;
            }
        }
    }
}
