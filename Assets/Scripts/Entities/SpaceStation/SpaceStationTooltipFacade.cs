using System.Collections.Generic;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Services.Tooltip;

namespace EmpireAtWar.Entities.SpaceStation
{
    public sealed class SpaceStationTooltipFacade : IEntityTooltipFacade
    {
        public TooltipContent Build(List<TooltipStat> stats, string status) =>
            new TooltipContent("Space station", "Produces units, research and upgrades. Select to open the station roster.",
                stats: stats, status: status);
    }
}
