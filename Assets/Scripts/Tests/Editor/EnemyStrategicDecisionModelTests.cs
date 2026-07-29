using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class EnemyStrategicDecisionModelTests
    {
        [Test]
        public void FleetObjective_HuntsEnemyFleet()
        {
            EnemyStrategicDecisionModel model = new EnemyStrategicDecisionModel();
            EnemyStrategicSnapshot snapshot = new EnemyStrategicSnapshot(
                BattleVictoryCondition.DestroyEnemyFleet,
                EnemyAiDifficulty.Hard,
                5,
                3,
                true,
                true,
                false);

            EnemyStrategicDecision decision = model.Evaluate(snapshot);

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.HuntFleet));
        }

        [Test]
        public void BaseObjective_AssaultsWhenDifficultyThresholdIsMet()
        {
            EnemyStrategicDecisionModel model = new EnemyStrategicDecisionModel();
            EnemyStrategicSnapshot snapshot = new EnemyStrategicSnapshot(
                BattleVictoryCondition.DestroyOpponentBase,
                EnemyAiDifficulty.UltraHard,
                3,
                3,
                true,
                true,
                false);

            EnemyStrategicDecision decision = model.Evaluate(snapshot);

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.AssaultBase));
        }

        [Test]
        public void EasyDifficulty_CapturesZoneUntilSaferBaseThresholdIsMet()
        {
            EnemyStrategicDecisionModel model = new EnemyStrategicDecisionModel();
            EnemyStrategicSnapshot snapshot = new EnemyStrategicSnapshot(
                BattleVictoryCondition.DestroyOpponentBase,
                EnemyAiDifficulty.Easy,
                3,
                3,
                true,
                true,
                false);

            EnemyStrategicDecision decision = model.Evaluate(snapshot);

            Assert.That(decision.State, Is.EqualTo(EnemyStrategicState.CaptureZone));
        }

        [Test]
        public void Difficulty_ChangesCommittedFleetSize()
        {
            EnemyStrategicDecisionModel model = new EnemyStrategicDecisionModel();

            EnemyStrategicDecision easy = model.Evaluate(new EnemyStrategicSnapshot(
                BattleVictoryCondition.DestroyEnemyFleet,
                EnemyAiDifficulty.Easy,
                10,
                1,
                false,
                false,
                false));
            EnemyStrategicDecision ultra = model.Evaluate(new EnemyStrategicSnapshot(
                BattleVictoryCondition.DestroyEnemyFleet,
                EnemyAiDifficulty.UltraHard,
                10,
                1,
                false,
                false,
                false));

            Assert.That(easy.CommittedShipCount, Is.EqualTo(5));
            Assert.That(ultra.CommittedShipCount, Is.EqualTo(10));
        }
    }
}
