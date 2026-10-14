using System.Collections.Generic;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Entities.Game
{
    public sealed class SurvivalOutcomeRule : IBattleOutcomeRule
    {
        private readonly SurvivalModel _model;
        private readonly PlayerId _stationOwner;
        public SurvivalOutcomeRule(SurvivalModel model, BattleDefinition battle)
        {
            _model = model;
            _stationOwner = battle.Human.Slot.Id;
        }

        public BattleOutcome Evaluate(BattleVictoryCondition condition,
            IReadOnlyList<PlayerBattleState> players, TeamId localTeam)
        {
            foreach (PlayerBattleState player in players)
                if (player.Player == _stationOwner && !player.IsBaseAlive) _model.Lose();
            return _model.Phase == SurvivalPhase.Lost ? BattleOutcome.EnemyVictory :
                _model.Phase == SurvivalPhase.Won ? BattleOutcome.PlayerVictory : BattleOutcome.None;
        }
    }
}
