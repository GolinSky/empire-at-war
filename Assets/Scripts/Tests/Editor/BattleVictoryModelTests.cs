using EmpireAtWar.Entities.Game;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class BattleVictoryModelTests
    {
        [Test]
        public void FleetCondition_DoesNotFinishBeforeFleetWasObserved()
        {
            BattleVictoryModel model = new BattleVictoryModel();

            BattleOutcome outcome = model.Evaluate(
                BattleVictoryCondition.DestroyEnemyFleet,
                0,
                0,
                true,
                true);

            Assert.That(outcome, Is.EqualTo(BattleOutcome.None));
        }

        [Test]
        public void FleetCondition_ReturnsPlayerVictoryAfterEnemyFleetIsDestroyed()
        {
            BattleVictoryModel model = new BattleVictoryModel();
            model.Evaluate(BattleVictoryCondition.DestroyEnemyFleet, 2, 2, true, true);

            BattleOutcome outcome = model.Evaluate(
                BattleVictoryCondition.DestroyEnemyFleet,
                2,
                0,
                true,
                true);

            Assert.That(outcome, Is.EqualTo(BattleOutcome.PlayerVictory));
        }

        [Test]
        public void BaseCondition_IgnoresFleetCount()
        {
            BattleVictoryModel model = new BattleVictoryModel();
            model.Evaluate(BattleVictoryCondition.DestroyOpponentBase, 0, 4, true, true);

            BattleOutcome outcome = model.Evaluate(
                BattleVictoryCondition.DestroyOpponentBase,
                0,
                4,
                true,
                false);

            Assert.That(outcome, Is.EqualTo(BattleOutcome.PlayerVictory));
        }
    }
}
