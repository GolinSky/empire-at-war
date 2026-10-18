using System;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Services.VContainer
{
    public static class BattleModes
    {
        public static IBattleMode Create(BattleDefinition battle, IAssetService assets) => battle.Outcome switch
        {
            OutcomeRule.Skirmish => new SkirmishMode(),
            OutcomeRule.Survival => new SurvivalMode(assets),
            OutcomeRule.Simulation => new SimulationMode(battle),
            _ => throw new ArgumentOutOfRangeException(nameof(battle), battle.Outcome, "Unknown outcome rule.")
        };
    }
}
