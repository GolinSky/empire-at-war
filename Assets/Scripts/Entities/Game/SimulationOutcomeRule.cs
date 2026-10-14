using System.Collections.Generic;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Entities.Game
{
    public sealed class SimulationOutcomeRule : IBattleOutcomeRule
    {
        private readonly SimulationModel _model;
        public SimulationOutcomeRule(SimulationModel model) => _model = model;
        public BattleOutcome Evaluate(BattleVictoryCondition condition, IReadOnlyList<PlayerBattleState> players, TeamId localTeam) =>
            _model.Result == null ? BattleOutcome.None : _model.Result.Outcome;
    }
}
