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
    public sealed class BattleVictoryModel : Model
    {
        private readonly HashSet<PlayerId> _observedBases = new HashSet<PlayerId>();
        private readonly HashSet<TeamId> _survivingTeams = new HashSet<TeamId>();

        public BattleOutcome Evaluate(
            BattleVictoryCondition victoryCondition,
            IReadOnlyList<PlayerBattleState> players,
            TeamId localTeam)
        {
            foreach (PlayerBattleState player in players)
            {
                if (player.ShipCount < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(players), $"{player.Player} has a negative ship count.");
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
                // Fleet victory also waits for ships and for AI builds still on the way.
                BattleVictoryCondition.DestroyEnemyFleet =>
                    player.ShipCount == 0 && !player.HasPendingReinforcement,
                BattleVictoryCondition.DestroyOpponentBase => true,
                _ => throw new ArgumentOutOfRangeException(nameof(victoryCondition))
            };
        }
    }
}
