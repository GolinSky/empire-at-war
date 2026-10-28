using System.Collections.Generic;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Services.Tooltip;

namespace EmpireAtWar.Entities.DefendPlatform
{
    public sealed class DefendPlatformTooltipFacade : IEntityTooltipFacade
    {
        private readonly DefendPlatformCatalog _catalog;
        private readonly DefendPlatformType _type;

        public DefendPlatformTooltipFacade(DefendPlatformCatalog catalog, DefendPlatformType type)
        {
            _catalog = catalog;
            _type = type;
        }

        public TooltipContent Build(List<TooltipStat> stats, string status) =>
            new TooltipContent(title: "Defensive platform", description: "Defends the surrounding area.", stats: stats,
                status: status, populationCost: _catalog.Get(_type).UnitCapacity);
    }
}
