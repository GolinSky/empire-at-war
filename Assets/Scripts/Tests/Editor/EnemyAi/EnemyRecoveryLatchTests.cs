using EmpireAtWar.Entities.EnemyFaction.Models;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("EnemyAi")]
    public sealed class EnemyRecoveryLatchTests
    {
        [Test]
        public void WeakFleetRequiresSustainedAttack()
        {
            var latch = new EnemyRecoveryLatch();
            latch.Update(0f, 0.5f, 0.8f, true, false, false);
            latch.Update(4f, 0.5f, 0.8f, true, false, false);
            Assert.That(latch.IsActive, Is.False);
            latch.Update(5f, 0.5f, 0.8f, true, false, false);
            Assert.That(latch.IsActive, Is.True);
            Assert.That(latch.Trigger, Is.EqualTo("fleetWeak"));
        }

        [TestCase(true, false, "stationLost")]
        [TestCase(false, true, "critical")]
        public void EmergencyEntersImmediately(bool lost, bool critical, string trigger)
        {
            var latch = new EnemyRecoveryLatch();
            latch.Update(0f, 1.1f, 0.8f, false, lost, critical);
            Assert.That(latch.IsActive, Is.True);
            Assert.That(latch.Trigger, Is.EqualTo(trigger));
        }

        [Test]
        public void RecoveryRequiresTwentyQuietSecondsAtParity()
        {
            var latch = new EnemyRecoveryLatch();
            latch.Update(0f, 0.4f, 0.8f, true, false, true);
            latch.Update(1f, 1f, 0.8f, false, false, false);
            latch.Update(20f, 1f, 0.8f, false, false, false);
            Assert.That(latch.IsActive, Is.True);
            latch.Update(21f, 1f, 0.8f, false, false, false);
            Assert.That(latch.IsActive, Is.False);
            Assert.That(latch.Trigger, Is.EqualTo("recovered"));
        }

        [Test]
        public void ReplacementEndsRecoveryWithoutImmediatelyRelatchingStationLoss()
        {
            var latch = new EnemyRecoveryLatch();
            latch.Update(0f, 0.4f, 0.8f, false, true, false);
            latch.StationBuilt();
            latch.Update(1f, 0.4f, 0.8f, false, true, false);
            Assert.That(latch.IsActive, Is.False);
        }
    }
}
