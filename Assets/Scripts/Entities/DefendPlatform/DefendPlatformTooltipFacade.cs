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
            _type == DefendPlatformType.BattleAsteroid
                ? new TooltipContent(title: "Battle Asteroid", description: "Defends the surrounding area.", stats: stats,
                    status: status)
                : UnitTooltipContent.Build(_catalog.Get(_type), stats, status: status);
    }
}
