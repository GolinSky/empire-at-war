using System.Collections.Generic;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Services.Tooltip;

namespace EmpireAtWar.Entities.DefendPlatform
{
    public sealed class DefendPlatformTooltipFacade : IEntityTooltipFacade
    {
        public TooltipContent Build(List<TooltipStat> stats, string status) =>
            new TooltipContent("Defensive platform", "Defends the surrounding area.", stats: stats, status: status);
    }
}
