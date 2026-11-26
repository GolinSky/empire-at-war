using System.Collections.Generic;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Entities.Units.Matchups;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.Tooltip;

namespace EmpireAtWar.Entities.Ship.EntityFacades
{
    public sealed class ShipTooltipFacade : IEntityTooltipFacade
    {
        private readonly UnitSheet _sheet;
        private readonly MatchupRules _matchups;
        private readonly IHealthTooltipObserver _health;

        public ShipTooltipFacade(IUnitSheetCatalog sheets, MatchupRules matchups, IHealthTooltipObserver health,
            ShipType shipType)
        {
            _sheet = sheets.Get(UnitTypeId.Ship(shipType));
            _matchups = matchups;
            _health = health;
        }

        public TooltipContent Build(List<TooltipStat> stats, string status)
        {
            var rows = new List<TooltipStat>();
            UnitStatRows.Add(rows, _sheet, _health);
            rows.AddRange(stats);
            return UnitTooltipContent.Build(_sheet, _matchups, rows, status: status);
        }
    }
}
