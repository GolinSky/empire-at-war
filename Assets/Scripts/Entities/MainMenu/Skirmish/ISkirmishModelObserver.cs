using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;

namespace EmpireAtWar.Entities.MainMenu.Skirmish
{
    public interface ISkirmishModelObserver
    {
        event Action Changed;

        IReadOnlyList<SkirmishSlotSetup> Slots { get; }
        int TeamCount { get; }
        int ActivePlayerCount { get; }
        int ActiveTeamCount { get; }
        PlanetType Planet { get; }
        MapSize MapSize { get; }
        BattleVictoryCondition VictoryCondition { get; }
        float MinStartingMoney { get; }
        float MaxStartingMoney { get; }
        float StartingMoney { get; }
        bool CanStart { get; }
    }
}
