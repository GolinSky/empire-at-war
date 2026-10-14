using System.Collections.Generic;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Entities.Game
{
    public interface IBattleOutcomeRule
    {
        BattleOutcome Evaluate(BattleVictoryCondition victoryCondition,
            IReadOnlyList<PlayerBattleState> players, TeamId localTeam);
    }
}
