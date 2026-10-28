using System.Collections.Generic;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Tooltip;

namespace EmpireAtWar.Entities.SpaceStation
{
    public sealed class SpaceStationTooltipFacade : IEntityTooltipFacade
    {
        private readonly IFactionLevelObserver _level;

        public SpaceStationTooltipFacade(IFactionLevelObserver level) => _level = level;

        public TooltipContent Build(List<TooltipStat> stats, string status) =>
            new TooltipContent(title: "Space station", description: "Produces units, research and upgrades. Select to open the station roster.",
                subtitle: $"Level {_level.CurrentLevel}", stats: stats, status: status);
    }
}
