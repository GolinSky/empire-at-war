using EmpireAtWar.Entities.Game;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class BattleVictoryModelTests
    {
        [Test]
        public void FleetCondition_DoesNotFinishBeforeBaseWasObserved()
        {
            BattleVictoryModel model = new BattleVictoryModel();

            BattleOutcome outcome = model.Evaluate(
                BattleVictoryCondition.DestroyEnemyFleet,
                0,
                0,
                false,
                false,
                false);

            Assert.That(outcome, Is.EqualTo(BattleOutcome.None));
        }

        [Test]
        public void FleetCondition_DoesNotFinishWhileEnemyStationIsAlive()
        {
            BattleVictoryModel model = new BattleVictoryModel();
            model.Evaluate(BattleVictoryCondition.DestroyEnemyFleet, 2, 2, true, true, false);

            BattleOutcome outcome = model.Evaluate(
                BattleVictoryCondition.DestroyEnemyFleet,
                2,
                0,
                true,
                true,
                false);

            Assert.That(outcome, Is.EqualTo(BattleOutcome.None));
        }

        [Test]
        public void FleetCondition_DoesNotFinishWhileEnemyHasPendingReinforcement()
        {
            BattleVictoryModel model = new BattleVictoryModel();
            model.Evaluate(BattleVictoryCondition.DestroyEnemyFleet, 2, 2, true, true, false);

            BattleOutcome outcome = model.Evaluate(
                BattleVictoryCondition.DestroyEnemyFleet,
                2,
                0,
                true,
                false,
                true);

            Assert.That(outcome, Is.EqualTo(BattleOutcome.None));
        }

        [Test]
        public void FleetCondition_ReturnsPlayerVictoryAfterEnemyShipsStationAndReinforcementsAreGone()
        {
            BattleVictoryModel model = new BattleVictoryModel();
            model.Evaluate(BattleVictoryCondition.DestroyEnemyFleet, 2, 2, true, true, false);

            BattleOutcome outcome = model.Evaluate(
                BattleVictoryCondition.DestroyEnemyFleet,
                2,
                0,
                true,
                false,
                false);

            Assert.That(outcome, Is.EqualTo(BattleOutcome.PlayerVictory));
        }

        [Test]
        public void FleetCondition_ReturnsEnemyVictoryAfterPlayerShipsAndStationAreGone()
        {
            BattleVictoryModel model = new BattleVictoryModel();
            model.Evaluate(BattleVictoryCondition.DestroyEnemyFleet, 2, 2, true, true, false);

            BattleOutcome outcome = model.Evaluate(
                BattleVictoryCondition.DestroyEnemyFleet,
                0,
                2,
                false,
                true,
                false);

            Assert.That(outcome, Is.EqualTo(BattleOutcome.EnemyVictory));
        }

        [Test]
        public void BaseCondition_IgnoresFleetCount()
        {
            BattleVictoryModel model = new BattleVictoryModel();
            model.Evaluate(BattleVictoryCondition.DestroyOpponentBase, 0, 4, true, true, false);

            BattleOutcome outcome = model.Evaluate(
                BattleVictoryCondition.DestroyOpponentBase,
                0,
                4,
                true,
                false,
                true);

            Assert.That(outcome, Is.EqualTo(BattleOutcome.PlayerVictory));
        }
    }
}
