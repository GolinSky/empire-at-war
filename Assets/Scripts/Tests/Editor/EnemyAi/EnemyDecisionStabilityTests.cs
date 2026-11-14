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
        public void Smoothing_DampsAlternatingMeasurementsAndIgnoresDuplicateTick()
        {
            var smoother = new EnemyAdvantageSmoother();
            smoother.Sample(0.9f, 0.9f, 0f);
            for (int i = 1; i <= 20; i++)
            {
                smoother.Sample(i % 2 == 0 ? 0.78f : 1.03f, 1f, i);
                Assert.That(smoother.Local, Is.InRange(0.8f, 1f));
            }
            float previous = smoother.Local;
            smoother.Sample(10f, 10f, 20f);
            Assert.That(smoother.Local, Is.EqualTo(previous));
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
        public void Retreat_LeavesOnlyAfterExitThresholdAndMinimumHold()
        {
            var model = new EnemyStrategicDecisionModel();
            float cutoff = EnemyAiDifficultyProfile.Get(EnemyAiDifficulty.Medium).RetreatAdvantage;
            Assert.That(model.Evaluate(Snapshot(cutoff - 0.05f), now: 0f).State,
                Is.EqualTo(EnemyStrategicState.RetreatValue));
            Assert.That(model.Evaluate(Snapshot(cutoff * 1.1f), now: 20f).State,
                Is.EqualTo(EnemyStrategicState.RetreatValue));
            Assert.That(model.Evaluate(Snapshot(cutoff * 1.25f), now: 21f).State,
                Is.EqualTo(EnemyStrategicState.EngageLocal));
            Assert.That(model.Evaluate(Snapshot(cutoff - 0.05f), now: 22f).State,
                Is.EqualTo(EnemyStrategicState.EngageLocal));
            Assert.That(model.SwitchHeld, Is.True);
            Assert.That(model.Evaluate(Snapshot(cutoff - 0.05f), now: 23f).State,
                Is.EqualTo(EnemyStrategicState.EngageLocal));
            Assert.That(model.SwitchHeld, Is.False);
            Assert.That(model.Evaluate(Snapshot(cutoff - 0.05f), now: 30f).State,
                Is.EqualTo(EnemyStrategicState.RetreatValue));
        }

        [Test]
        public void CorneredAndNoShips_BypassHoldImmediately()
        {
            var model = new EnemyStrategicDecisionModel();
            model.Evaluate(Snapshot(0.1f), now: 0f);
            Assert.That(model.Evaluate(Snapshot(0.1f, safe: false), now: 1f).State,
                Is.EqualTo(EnemyStrategicState.EngageLocal));
            Assert.That(model.Bypass, Is.EqualTo("cornered"));
            Assert.That(model.Evaluate(Snapshot(0.1f, ships: 0), now: 2f).State,
                Is.EqualTo(EnemyStrategicState.RebuildFleet));
            Assert.That(model.Bypass, Is.EqualTo("noShips"));
        }

        [Test]
        public void Preview_DoesNotStartStateCommitment()
        {
            var model = new EnemyStrategicDecisionModel();
            model.Evaluate(Snapshot(0.1f), now: 0f, commit: false);
            Assert.That(model.Evaluate(Snapshot(1f), now: 1f).State,
                Is.EqualTo(EnemyStrategicState.EngageLocal));
            Assert.That(model.IsRecovering, Is.False);
        }

        [TestCase(false, EnemyStrategicState.RetreatValue)]
        [TestCase(true, EnemyStrategicState.CaptureZone)]
        public void Recovery_OverridesDefenseWithAnchorOrRelay(bool relay, EnemyStrategicState expected)
        {
            var snapshot = new EnemyStrategicSnapshot(BattleVictoryCondition.DestroyEnemyFleet,
                EnemyAiDifficulty.Medium, 2, 4, 0, 0.5f, 5f, relay, false, true,
                localAdvantage: 0.5f, hasLocalThreat: true, hasStationPressure: true,
                hasSafeRetreat: !relay, recoveryMode: true, recoveryCapturePending: relay);
            Assert.That(new EnemyStrategicDecisionModel().Evaluate(snapshot, now: 0f).State, Is.EqualTo(expected));
        }

        private static EnemyStrategicSnapshot Snapshot(float local, bool safe = true, int ships = 2) =>
            new EnemyStrategicSnapshot(BattleVictoryCondition.DestroyEnemyFleet, EnemyAiDifficulty.Medium,
                ships, 4, 0, 0.5f, 0f, false, false, true,
                localAdvantage: local, hasLocalThreat: true, hasSafeRetreat: safe);
    }
}
