using System;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Networking.Online;

namespace EmpireAtWar.Services.VContainer
{
    public static class BattleModes
    {
        public static IBattleMode Create(BattleDefinition battle, IAssetService assets, OnlineMatchService online) => battle.Outcome switch
        {
            OutcomeRule.Skirmish => new SkirmishMode(online),
            OutcomeRule.Survival => new SurvivalMode(assets, online),
            OutcomeRule.Simulation => new SimulationMode(battle),
            _ => throw new ArgumentOutOfRangeException(nameof(battle), battle.Outcome, "Unknown outcome rule.")
        };
    }
}
