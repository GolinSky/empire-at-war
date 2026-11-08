using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Entities.Game
{
    public sealed class SurvivalOutcomeRule : IBattleOutcomeRule
    {
        private readonly SurvivalModel _model;
        private readonly HashSet<PlayerId> _stationOwners;
        public SurvivalOutcomeRule(SurvivalModel model, BattleDefinition battle)
        {
            _model = model;
            _stationOwners = new HashSet<PlayerId>(battle.Humans.Select(human => human.Slot.Id));
        }

        /// <summary>The defenders lose once every human station has fallen.</summary>
        public BattleOutcome Evaluate(BattleVictoryCondition condition,
            IReadOnlyList<PlayerBattleState> players, TeamId localTeam)
        {
            bool anyStationAlive = false;
            foreach (PlayerBattleState player in players)
                if (_stationOwners.Contains(player.Player) && player.IsBaseAlive) anyStationAlive = true;
            if (!anyStationAlive) _model.Lose();
            return _model.Phase == SurvivalPhase.Lost ? BattleOutcome.EnemyVictory :
                _model.Phase == SurvivalPhase.Won ? BattleOutcome.PlayerVictory : BattleOutcome.None;
        }
    }
}
