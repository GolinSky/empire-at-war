using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Services.Tooltip;

namespace EmpireAtWar.Entities.Tooltip
{
    public interface IEntityTooltipFacade : IEntityFacade
    {
        TooltipContent Build(List<TooltipStat> stats, string status);
    }
}
