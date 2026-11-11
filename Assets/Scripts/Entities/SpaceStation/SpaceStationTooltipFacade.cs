using System.Collections.Generic;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Tooltip;

namespace EmpireAtWar.Entities.SpaceStation
{
    public sealed class SpaceStationTooltipFacade : IEntityTooltipFacade
    {
        private readonly IFactionLevelObserver _level;

        private readonly string _iconKey;

        public SpaceStationTooltipFacade(IFactionLevelObserver level, StationLevelData stationLevels, FactionType faction)
        {
            _level = level;
            _iconKey = stationLevels.GetStationIconKey(faction);
        }

        public TooltipContent Build(List<TooltipStat> stats, string status) =>
            new TooltipContent(title: "Space station", description: "Produces units, research and upgrades. Select to open the station roster.",
                iconKey: _iconKey, subtitle: $"Level {_level.CurrentLevel}", stats: stats, status: status);
    }
}
