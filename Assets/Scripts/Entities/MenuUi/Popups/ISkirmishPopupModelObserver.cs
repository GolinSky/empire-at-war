using System;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Entities.MenuUi.Popups
{
    public interface ISkirmishPopupModelObserver
    {
        event Action Changed;

        FactionType PlayerFaction { get; }
        FactionType EnemyFaction { get; }
        PlanetType Planet { get; }
        MapSize MapSize { get; }
        BattleVictoryCondition VictoryCondition { get; }
        EnemyAiDifficulty EnemyDifficulty { get; }
        float MinStartingMoney { get; }
        float MaxStartingMoney { get; }
        float StartingMoney { get; }
    }
}
