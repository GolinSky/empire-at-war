using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Entities.Units.Matchups;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.Tooltip;

namespace EmpireAtWar.Entities.Squadrons.EntityFacades
{
    public sealed class SquadronTooltipFacade : IEntityTooltipFacade
    {
        private readonly IHealthModelObserver _members;
        private readonly IHealthTooltipObserver _health;
        private readonly UnitSheet _sheet;
        private readonly MatchupRules _matchups;

        public SquadronTooltipFacade(IHealthModelObserver members, IHealthTooltipObserver health, IUnitSheetCatalog sheets,
            MatchupRules matchups, SquadronType squadronType)
        {
            _members = members;
            _health = health;
            _sheet = sheets.Get(UnitTypeId.Squadron(squadronType));
            _matchups = matchups;
        }

        public TooltipContent Build(List<TooltipStat> stats, string status)
        {
            var rows = new List<TooltipStat>();
            UnitStatRows.Add(rows, _sheet, _health);
            rows.Add(new TooltipStat(label: "Surviving fighters", current: _members.HardPointModels.Count(member => !member.IsDestroyed),
                max: _members.HardPointModels.Count));
            rows.AddRange(stats);
            return UnitTooltipContent.Build(_sheet, _matchups, rows, status: status);
        }
    }
}
