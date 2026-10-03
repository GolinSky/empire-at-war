using System.Collections.Generic;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Tooltip;

namespace EmpireAtWar.Entities.Tooltip
{
    public static class UnitTooltipContent
    {
        public static TooltipContent Build(FactionData data, IEnumerable<TooltipStat> stats,
            IEnumerable<TooltipRequirement> requirements = null, string status = "") =>
            new TooltipContent(title: data.Name, description: data.Description, iconKey: data.IconKey, subtitle: data.Role,
                stats: stats,
                strongAgainst: data.Matchups != null ? data.Matchups.StrongAgainst : null,
                weakAgainst: data.Matchups != null ? data.Matchups.WeakAgainst : null,
                requirements: requirements, status: status);
    }
}
