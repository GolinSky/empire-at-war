using EmpireAtWar.Services.TacticalMarkers;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("EnemyAi")]
    public sealed class AllyAiMarkerDecisionTests
    {
        [Test]
        public void LookIsAnIntelHintEvenWithoutAvailableShips()
        {
            Assert.That(AllyAiMarkerDecision.Evaluate(TacticalMarkerType.Look, false, 0f, true, true, false),
                Is.EqualTo(TacticalMarkerReplyStatus.Accepted));
        }

        [TestCase(TacticalMarkerType.Attack)]
        [TestCase(TacticalMarkerType.Capture)]
        [TestCase(TacticalMarkerType.Defend)]
        [TestCase(TacticalMarkerType.Rally)]
        public void RequestWithInferiorForceIsDeclined(TacticalMarkerType type)
        {
            Assert.That(AllyAiMarkerDecision.Evaluate(type, true, 0.5f, false, false, true),
                Is.EqualTo(TacticalMarkerReplyStatus.Declined));
        }

        [TestCase(TacticalMarkerType.Attack)]
        [TestCase(TacticalMarkerType.Capture)]
        [TestCase(TacticalMarkerType.Defend)]
        [TestCase(TacticalMarkerType.Rally)]
        public void SafeNearbyRequestIsAccepted(TacticalMarkerType type)
        {
            Assert.That(AllyAiMarkerDecision.Evaluate(type, true, 2f, false, false, true),
                Is.EqualTo(TacticalMarkerReplyStatus.Accepted));
        }

        [TestCase(false, false, false)]
        [TestCase(true, true, false)]
        [TestCase(true, false, true)]
        public void MissingForceHomeDefenseAndDistanceDelayRequests(bool hasForce, bool homeThreatened, bool distant)
        {
            Assert.That(AllyAiMarkerDecision.Evaluate(TacticalMarkerType.Attack, hasForce, 2f,
                homeThreatened, distant, true), Is.EqualTo(TacticalMarkerReplyStatus.Delayed));
        }

        [TestCase(true, 0.5f, TacticalMarkerReplyStatus.Accepted)]
        [TestCase(false, 0.5f, TacticalMarkerReplyStatus.Declined)]
        [TestCase(true, 2f, TacticalMarkerReplyStatus.Declined)]
        public void RetreatRequiresLocalShipsLosingTheFight(bool localShips, float advantage,
            TacticalMarkerReplyStatus expected)
        {
            Assert.That(AllyAiMarkerDecision.Evaluate(TacticalMarkerType.Retreat, true, advantage,
                false, false, localShips), Is.EqualTo(expected));
        }
    }
}
