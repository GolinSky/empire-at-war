using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;

namespace EmpireAtWar.Entities.MenuUi.Popups
{
    public interface ISkirmishPopupModelObserver
    {
        event Action Changed;

        IReadOnlyList<SkirmishSlotSetup> Slots { get; }
        int TeamCount { get; }
        PlanetType Planet { get; }
        MapSize MapSize { get; }
        BattleVictoryCondition VictoryCondition { get; }
        float MinStartingMoney { get; }
        float MaxStartingMoney { get; }
        float StartingMoney { get; }
        bool CanStart { get; }
    }
}
