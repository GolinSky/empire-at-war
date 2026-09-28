using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Players;
using NUnit.Framework;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class BattleVictoryModelTests
    {
        private static readonly TeamId HUMAN_TEAM = new TeamId(0);
        private static readonly TeamId ENEMY_TEAM = new TeamId(1);

        [Test]
        public void FleetCondition_DoesNotFinishBeforeBaseWasObserved()
        {
            BattleVictoryModel model = new BattleVictoryModel();

            BattleOutcome outcome = Evaluate(
                model,
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
            Evaluate(model, BattleVictoryCondition.DestroyEnemyFleet, 2, 2, true, true, false);

            BattleOutcome outcome = Evaluate(
                model,
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
            Evaluate(model, BattleVictoryCondition.DestroyEnemyFleet, 2, 2, true, true, false);

            BattleOutcome outcome = Evaluate(
                model,
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
            Evaluate(model, BattleVictoryCondition.DestroyEnemyFleet, 2, 2, true, true, false);

            BattleOutcome outcome = Evaluate(
                model,
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
            Evaluate(model, BattleVictoryCondition.DestroyEnemyFleet, 2, 2, true, true, false);

            BattleOutcome outcome = Evaluate(
                model,
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
            Evaluate(model, BattleVictoryCondition.DestroyOpponentBase, 0, 4, true, true, false);

            BattleOutcome outcome = Evaluate(
                model,
                BattleVictoryCondition.DestroyOpponentBase,
                0,
                4,
                true,
                false,
                true);

            Assert.That(outcome, Is.EqualTo(BattleOutcome.PlayerVictory));
        }

        [Test]
        public void TeamGame_FallenPlayerWithLivingAlly_KeepsBattleRunning()
        {
            BattleVictoryModel model = new BattleVictoryModel();
            model.Evaluate(BattleVictoryCondition.DestroyOpponentBase, TeamStates(true, true, true, true), HUMAN_TEAM);

            BattleOutcome outcome = model.Evaluate(
                BattleVictoryCondition.DestroyOpponentBase, TeamStates(false, true, true, true), HUMAN_TEAM);

            Assert.That(outcome, Is.EqualTo(BattleOutcome.None));
        }

        [Test]
        public void TeamGame_BothEnemiesFallen_ReturnsPlayerVictoryEvenIfHumanFell()
        {
            BattleVictoryModel model = new BattleVictoryModel();
            model.Evaluate(BattleVictoryCondition.DestroyOpponentBase, TeamStates(true, true, true, true), HUMAN_TEAM);

            BattleOutcome outcome = model.Evaluate(
                BattleVictoryCondition.DestroyOpponentBase, TeamStates(false, false, true, false), HUMAN_TEAM);

            Assert.That(outcome, Is.EqualTo(BattleOutcome.PlayerVictory));
        }

        [Test]
        public void FreeForAll_WinsOnlyAfterEveryOtherTeamFalls()
        {
            BattleVictoryModel model = new BattleVictoryModel();
            model.Evaluate(BattleVictoryCondition.DestroyOpponentBase, FreeForAllStates(true, true, true), HUMAN_TEAM);

            BattleOutcome afterFirst = model.Evaluate(
                BattleVictoryCondition.DestroyOpponentBase, FreeForAllStates(true, false, true), HUMAN_TEAM);
            BattleOutcome afterSecond = model.Evaluate(
                BattleVictoryCondition.DestroyOpponentBase, FreeForAllStates(true, false, false), HUMAN_TEAM);

            Assert.That(afterFirst, Is.EqualTo(BattleOutcome.None));
            Assert.That(afterSecond, Is.EqualTo(BattleOutcome.PlayerVictory));
        }

        // Keeps the duel cases readable: the human is team 0, the AI team 1.
        private static BattleOutcome Evaluate(
            BattleVictoryModel model,
            BattleVictoryCondition condition,
            int playerShipCount,
            int enemyShipCount,
            bool isPlayerBaseAlive,
            bool isEnemyBaseAlive,
            bool hasEnemyPendingReinforcement)
        {
            return model.Evaluate(condition, new[]
            {
                new PlayerBattleState(TestPlayers.Human, HUMAN_TEAM, playerShipCount, isPlayerBaseAlive, false),
                new PlayerBattleState(TestPlayers.Enemy, ENEMY_TEAM, enemyShipCount, isEnemyBaseAlive,
                    hasEnemyPendingReinforcement)
            }, HUMAN_TEAM);
        }

        private static PlayerBattleState[] TeamStates(bool human, bool enemy, bool ally, bool secondEnemy)
        {
            return new[]
            {
                new PlayerBattleState(TestPlayers.Human, HUMAN_TEAM, 0, human, false),
                new PlayerBattleState(TestPlayers.Enemy, ENEMY_TEAM, 0, enemy, false),
                new PlayerBattleState(TestPlayers.Ally, HUMAN_TEAM, 0, ally, false),
                new PlayerBattleState(TestPlayers.SecondEnemy, ENEMY_TEAM, 0, secondEnemy, false)
            };
        }

        private static PlayerBattleState[] FreeForAllStates(bool human, bool first, bool second)
        {
            return new[]
            {
                new PlayerBattleState(TestPlayers.Human, HUMAN_TEAM, 0, human, false),
                new PlayerBattleState(TestPlayers.Enemy, ENEMY_TEAM, 0, first, false),
                new PlayerBattleState(TestPlayers.Ally, new TeamId(2), 0, second, false)
            };
        }

    }
}
