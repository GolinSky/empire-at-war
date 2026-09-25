using System;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Entities.Game
{
    public sealed class BattleVictoryModel : PureModel
    {
        private bool _hasObservedPlayerBase;
        private bool _hasObservedEnemyBase;

        public BattleOutcome Evaluate(
            BattleVictoryCondition victoryCondition,
            int playerShipCount,
            int enemyShipCount,
            bool isPlayerBaseAlive,
            bool isEnemyBaseAlive,
            bool hasEnemyPendingReinforcement)
        {
            if (playerShipCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(playerShipCount));
            }

            if (enemyShipCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(enemyShipCount));
            }

            _hasObservedPlayerBase |= isPlayerBaseAlive;
            _hasObservedEnemyBase |= isEnemyBaseAlive;

            return victoryCondition switch
            {
                BattleVictoryCondition.DestroyEnemyFleet =>
                    EvaluateFleetOutcome(
                        playerShipCount,
                        enemyShipCount,
                        isPlayerBaseAlive,
                        isEnemyBaseAlive,
                        hasEnemyPendingReinforcement),
                BattleVictoryCondition.DestroyOpponentBase =>
                    EvaluateBaseOutcome(isPlayerBaseAlive, isEnemyBaseAlive),
                _ => throw new ArgumentOutOfRangeException(nameof(victoryCondition))
            };
        }

        // A side is wiped out only when its ships and station are gone; the enemy must also have no
        // queued reinforcements left. The player cannot deploy reinforcements without a station.
        private BattleOutcome EvaluateFleetOutcome(
            int playerShipCount,
            int enemyShipCount,
            bool isPlayerBaseAlive,
            bool isEnemyBaseAlive,
            bool hasEnemyPendingReinforcement)
        {
            bool isPlayerDefeated = _hasObservedPlayerBase && !isPlayerBaseAlive && playerShipCount == 0;
            bool isEnemyDefeated = _hasObservedEnemyBase && !isEnemyBaseAlive && enemyShipCount == 0 &&
                !hasEnemyPendingReinforcement;
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
