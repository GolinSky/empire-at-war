using EmpireAtWar.Services.Enemy;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [TestFixture, Category("EnemyAi")]
    public sealed class EnemyStationPressureTrackerTests
    {
        [Test]
        public void DamageWithoutVisibleAttackerLatchesAndReleasesAfterQuietPeriod()
        {
            var tracker = new EnemyStationPressureTracker();
            tracker.Update(1, 1000f, 1000f, 0f);
            tracker.Update(1, 996f, 1000f, 1f);
            Assert.That(tracker.HasPressure, Is.False);
            tracker.Update(1, 994f, 1000f, 2f);
            Assert.That(tracker.HasPressure, Is.True);
            tracker.Update(1, 993f, 1000f, 20f);
            tracker.Update(1, 993f, 1000f, 49f);
            Assert.That(tracker.HasPressure, Is.True);
            tracker.Update(1, 993f, 1000f, 50f);
            Assert.That(tracker.HasPressure, Is.False);
        }

        [Test]
        public void RepeatedGroupObservationAtSameTimePreservesLossRateAndBearing()
        {
            var tracker = new EnemyStationPressureTracker();
            tracker.Update(1, 1000f, 1000f, 0f);
            tracker.Update(1, 900f, 1000f, 1f, 2);
            tracker.ObserveThreatPosition(100f, 0f, 300f);
            float timeToLoss = tracker.EstimatedTimeToLossSeconds;
            tracker.Update(1, 900f, 1000f, 1f);
            Assert.That(tracker.HasPressure, Is.True);
            Assert.That(tracker.HasThreatPosition, Is.True);
            Assert.That(tracker.EstimatedTimeToLossSeconds, Is.EqualTo(timeToLoss));
            Assert.That(tracker.IsStationCritical, Is.True);
        }

        [Test]
        public void NewStationClearsPriorDamageAndKnownThreat()
        {
            var tracker = new EnemyStationPressureTracker();
            tracker.Update(1, 100f, 1000f, 0f, 2);
            tracker.ObserveThreatPosition(100f, 0f, 300f);
            tracker.Update(3, 1000f, 1000f, 1f);
            Assert.That(tracker.HasPressure, Is.False);
            Assert.That(tracker.HasThreatPosition, Is.False);
            Assert.That(tracker.EstimatedTimeToLossSeconds, Is.EqualTo(float.PositiveInfinity));
        }

        [Test]
        public void SmallLossesOutsideDamageWindowDoNotAccumulateIntoSiege()
        {
            var tracker = new EnemyStationPressureTracker();
            tracker.Update(1, 1000f, 1000f, 0f);
            tracker.Update(1, 997f, 1000f, 1f);
            tracker.Update(1, 994f, 1000f, 7f);
            Assert.That(tracker.HasPressure, Is.False);
        }
    }
}
