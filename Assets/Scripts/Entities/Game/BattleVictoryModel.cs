using System;
using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Entities.Game
{
    /// <summary>
    /// Players drop out one by one; a team loses when all of its players are defeated and the last
    /// team standing wins. The outcome is reported from the local player's point of view.
    /// </summary>
    public sealed class BattleVictoryModel : Model, IBattleOutcomeRule
    {
        private readonly HashSet<PlayerId> _observedBases = new HashSet<PlayerId>();
        private readonly HashSet<TeamId> _survivingTeams = new HashSet<TeamId>();

        /// <summary>Players whose station has been seen alive; only they can be defeated by losing it.</summary>
        public IReadOnlyCollection<PlayerId> ObservedBases => _observedBases;

        public void RestoreObservedBases(IEnumerable<PlayerId> players) => _observedBases.UnionWith(players);

        public BattleOutcome Evaluate(
            BattleVictoryCondition victoryCondition,
            IReadOnlyList<PlayerBattleState> players,
            TeamId localTeam)
        {
            foreach (PlayerBattleState player in players)
            {
                if (player.UnitCount < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(players), $"{player.Player} has a negative unit count.");
                }

                if (player.IsBaseAlive)
                {
                    _observedBases.Add(player.Player);
                }
            }

            _survivingTeams.Clear();
            foreach (PlayerBattleState player in players)
            {
                if (!IsDefeated(victoryCondition, player))
                {
                    _survivingTeams.Add(player.Team);
                }
            }

            if (_survivingTeams.Count == 0)
            {
                return BattleOutcome.Draw;
            }

            if (!_survivingTeams.Contains(localTeam))
            {
                return BattleOutcome.EnemyVictory;
            }

            return _survivingTeams.Count == 1 ? BattleOutcome.PlayerVictory : BattleOutcome.None;
        }

        // A player counts only after its station was seen alive, so a slow first spawn is not a defeat.
        private bool IsDefeated(BattleVictoryCondition victoryCondition, PlayerBattleState player)
        {
            if (!_observedBases.Contains(player.Player) || player.IsBaseAlive)
            {
                return false;
            }

            return victoryCondition switch
            {
                // Destroy-all also waits for construction and undeployed reserves, not for builds still in production.
                BattleVictoryCondition.DestroyEnemyFleet =>
                    player.UnitCount == 0 && !player.HasPendingReinforcement,
                BattleVictoryCondition.DestroyOpponentBase => true,
                _ => throw new ArgumentOutOfRangeException(nameof(victoryCondition))
            };
        }
    }
}
