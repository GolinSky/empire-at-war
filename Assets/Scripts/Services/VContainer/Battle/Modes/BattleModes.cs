using System;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Networking.Online;
using EmpireAtWar.Services.Tutorial.Guided;

namespace EmpireAtWar.Services.VContainer
{
    public static class BattleModes
    {
        public static IBattleMode Create(BattleDefinition battle, IAssetService assets, OnlineMatchService online) => battle.Mode switch
        {
            GameMode.Skirmish => new SkirmishMode(online),
            GameMode.Survival => new SurvivalMode(assets, online),
            GameMode.BattleSimulation => new SimulationMode(battle),
            GameMode.Tutorial => new TutorialMode(),
            _ => throw new ArgumentOutOfRangeException(nameof(battle), battle.Mode, "Unknown game mode.")
        };
    }
}
