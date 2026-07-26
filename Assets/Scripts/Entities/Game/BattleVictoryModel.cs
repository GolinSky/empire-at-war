using System;

namespace EmpireAtWar.Entities.Game
{
    public sealed class BattleVictoryModel
    {
        private bool _hasObservedPlayerFleet;
        private bool _hasObservedEnemyFleet;
        private bool _hasObservedPlayerBase;
        private bool _hasObservedEnemyBase;

        public BattleOutcome Evaluate(
            BattleVictoryCondition victoryCondition,
            int playerShipCount,
            int enemyShipCount,
            bool isPlayerBaseAlive,
            bool isEnemyBaseAlive)
        {
            if (playerShipCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(playerShipCount));
            }

            if (enemyShipCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(enemyShipCount));
            }

            _hasObservedPlayerFleet |= playerShipCount > 0;
            _hasObservedEnemyFleet |= enemyShipCount > 0;
            _hasObservedPlayerBase |= isPlayerBaseAlive;
            _hasObservedEnemyBase |= isEnemyBaseAlive;

            return victoryCondition switch
            {
                BattleVictoryCondition.DestroyEnemyFleet =>
                    EvaluateFleetOutcome(playerShipCount, enemyShipCount),
                BattleVictoryCondition.DestroyOpponentBase =>
                    EvaluateBaseOutcome(isPlayerBaseAlive, isEnemyBaseAlive),
                _ => throw new ArgumentOutOfRangeException(nameof(victoryCondition))
            };
        }

        private BattleOutcome EvaluateFleetOutcome(int playerShipCount, int enemyShipCount)
        {
            bool isPlayerDefeated = _hasObservedPlayerFleet && playerShipCount == 0;
            bool isEnemyDefeated = _hasObservedEnemyFleet && enemyShipCount == 0;
            return ResolveOutcome(isPlayerDefeated, isEnemyDefeated);
        }

        private BattleOutcome EvaluateBaseOutcome(bool isPlayerBaseAlive, bool isEnemyBaseAlive)
        {
            bool isPlayerDefeated = _hasObservedPlayerBase && !isPlayerBaseAlive;
            bool isEnemyDefeated = _hasObservedEnemyBase && !isEnemyBaseAlive;
            return ResolveOutcome(isPlayerDefeated, isEnemyDefeated);
        }

        private static BattleOutcome ResolveOutcome(bool isPlayerDefeated, bool isEnemyDefeated)
        {
            if (isPlayerDefeated && isEnemyDefeated)
            {
                return BattleOutcome.Draw;
            }

            if (isEnemyDefeated)
            {
                return BattleOutcome.PlayerVictory;
            }

            return isPlayerDefeated ? BattleOutcome.EnemyVictory : BattleOutcome.None;
        }
    }
}
