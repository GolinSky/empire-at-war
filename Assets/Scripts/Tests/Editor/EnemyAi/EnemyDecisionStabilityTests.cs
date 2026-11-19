using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    [Category("EnemyAi")]
    public sealed class EnemyDecisionStabilityTests
    {
        [Test]
        public void Smoothing_FirstContactDoesNotAverageAgainstNoThreatSentinel()
        {
            var smoother = new EnemyAdvantageSmoother();
            smoother.Sample(100f, 100f, 0f, hasLocalThreat: false, hasFleetThreat: false);
            smoother.Sample(0.5f, 0.6f, 1f);
            Assert.That(smoother.Local, Is.EqualTo(0.5f));
            Assert.That(smoother.Fleet, Is.EqualTo(0.6f));
            smoother.Sample(100f, 0.6f, 2f, hasLocalThreat: false);
            smoother.Sample(0.2f, 0.6f, 3f);
            Assert.That(smoother.Local, Is.EqualTo(0.2f));
            Assert.That(smoother.Fleet, Is.EqualTo(0.6f));
        }

        [Test]
        public void Smoothing_AppliesDropsImmediatelyAndDampsRecovery()
        {
            var smoother = new EnemyAdvantageSmoother();
            smoother.Sample(3f, 4f, 0f);
            smoother.Sample(0.2f, 0.4f, 1f);
            Assert.That(smoother.Local, Is.EqualTo(0.2f));
            Assert.That(smoother.Fleet, Is.EqualTo(0.4f));

            smoother.Sample(0.1f, 0.1f, 1f);
            Assert.That(smoother.Local, Is.EqualTo(0.2f));
            Assert.That(smoother.Fleet, Is.EqualTo(0.4f));

            smoother.Sample(3f, 4f, 2f);
            Assert.That(smoother.Local, Is.GreaterThan(0.2f).And.LessThan(3f));
            Assert.That(smoother.Fleet, Is.GreaterThan(0.4f).And.LessThan(4f));
        }

        [Test]
        public void Commitment_RequiresTimeAndMeaningfulImprovement()
        {
            var commitment = new EnemyTargetCommitment();
            Assert.That(commitment.Select("a", 100f, null, 0f, false, 0f), Is.EqualTo("a"));
            Assert.That(commitment.Select("b", 125f, "a", 100f, true, 9f), Is.EqualTo("a"));
            Assert.That(commitment.Select("b", 110f, "a", 100f, true, 11f), Is.EqualTo("a"));
            Assert.That(commitment.Select("b", 125f, "a", 100f, true, 12f), Is.EqualTo("b"));
            Assert.That(commitment.LastCause, Is.EqualTo("better"));
        }

        [Test]
        public void Commitment_InvalidTargetImmediatelyReplacedWithReason()
        {
            var commitment = new EnemyTargetCommitment();
            commitment.Select("a", 100f, null, 0f, false, 0f);
            Assert.That(commitment.Select("b", 5f, "a", 100f, false, 1f, "unsafe"), Is.EqualTo("b"));
            Assert.That(commitment.LastCause, Is.EqualTo("invalid:unsafe"));
        }

        [Test]
        public void Commitment_NegativeTravelCostUsesRelativeImprovement()
        {
            var commitment = new EnemyTargetCommitment();
            commitment.Select("a", -100f, null, 0f, false, 0f);
            Assert.That(commitment.Select("b", -90f, "a", -100f, true, 20f), Is.EqualTo("a"));
            Assert.That(commitment.Select("b", -75f, "a", -100f, true, 20f), Is.EqualTo("b"));
        }

        [Test]
        public void StandAndFight_DoesNotFlipBetweenRetreatAndLocalEngagement()
        {
            var model = new EnemyStrategicDecisionModel();
            float cutoff = EnemyAiDifficultyProfile.Get(EnemyAiDifficulty.Medium).RetreatAdvantage;
            Assert.That(model.Evaluate(Snapshot(cutoff - 0.05f), now: 0f).State,
                Is.EqualTo(EnemyStrategicState.EngageLocal));
            Assert.That(model.Evaluate(Snapshot(cutoff + 0.05f), now: 1f).State,
                Is.EqualTo(EnemyStrategicState.EngageLocal));
            Assert.That(model.Evaluate(Snapshot(cutoff - 0.05f), now: 2f).State,
                Is.EqualTo(EnemyStrategicState.EngageLocal));
        }

        [Test]
        public void Engagement_RemainsCommittedWhileTheThreatPersists()
        {
            var model = new EnemyStrategicDecisionModel();
            Assert.That(model.Evaluate(Snapshot(0.1f), now: 0f).State,
                Is.EqualTo(EnemyStrategicState.EngageLocal));
            EnemyStrategicSnapshot hasCapture = Snapshot(1.5f, hasCapture: true);
            Assert.That(model.Evaluate(hasCapture, now: 0.5f).State,
                Is.EqualTo(EnemyStrategicState.EngageLocal));
            Assert.That(model.SwitchHeld, Is.True);
            Assert.That(model.Evaluate(hasCapture, now: EnemyStrategicDecisionModel.MIN_STATE_SECONDS).State,
                Is.EqualTo(EnemyStrategicState.CaptureZone));
        }

        [Test]
        public void LossOfAllShips_EndsEngagementImmediately()
        {
            var model = new EnemyStrategicDecisionModel();
            model.Evaluate(Snapshot(0.1f), now: 0f);
            Assert.That(model.Evaluate(Snapshot(0.1f, ships: 0), now: 1f).State,
                Is.EqualTo(EnemyStrategicState.RebuildFleet));
            Assert.That(model.Bypass, Is.EqualTo("noShips"));
        }

        [Test]
        public void Preview_DoesNotStartStateCommitment()
        {
            var model = new EnemyStrategicDecisionModel();
            model.Evaluate(Snapshot(0.1f), now: 0f, commit: false);
            Assert.That(model.Evaluate(Snapshot(1.5f, hasCapture: true), now: 1f).State,
                Is.EqualTo(EnemyStrategicState.CaptureZone));
            Assert.That(model.SwitchHeld, Is.False);
        }

        [TestCase(false, EnemyStrategicState.EngageLocal)]
        [TestCase(true, EnemyStrategicState.DefendBase)]
        public void Recovery_DoesNotWithdrawOrParkShips(bool localParity, EnemyStrategicState expected)
        {
            var snapshot = new EnemyStrategicSnapshot(BattleVictoryCondition.DestroyEnemyFleet,
                EnemyAiDifficulty.Medium, 2, 4, 0, 0.5f, 5f, true, false, true,
                localAdvantage: localParity ? 1f : 0.5f, hasLocalThreat: true, hasStationPressure: true,
                hasSafeRetreat: true, recoveryMode: true);
            Assert.That(new EnemyStrategicDecisionModel().Evaluate(snapshot, now: 0f).State, Is.EqualTo(expected));
        }

        [Test]
        public void CaptureDetachment_RespondsImmediatelyToSuddenLocalWeakness()
        {
            var model = new EnemyStrategicDecisionModel();
            Assert.That(model.Evaluate(Snapshot(1.5f, hasCapture: true), captureDetachment: true, now: 0f).State,
                Is.EqualTo(EnemyStrategicState.CaptureZone));
            Assert.That(model.Evaluate(Snapshot(0.5f, hasCapture: true), captureDetachment: true, now: 1f).State,
                Is.EqualTo(EnemyStrategicState.EngageLocal));
            Assert.That(model.SwitchHeld, Is.False);
        }

        private static EnemyStrategicSnapshot Snapshot(float local, int ships = 2, bool hasCapture = false) =>
            new EnemyStrategicSnapshot(BattleVictoryCondition.DestroyEnemyFleet, EnemyAiDifficulty.Medium,
                ships, 4, 0, 0.5f, 0f, hasCapture, false, true,
                localAdvantage: local, hasLocalThreat: true, hasSafeRetreat: true);
    }
}
