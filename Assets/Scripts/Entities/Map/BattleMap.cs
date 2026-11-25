using System.Collections.Generic;
using EmpireAtWar.Components.Obstacles;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Views.ReinforcementZones;

namespace EmpireAtWar.Entities.Map
{
    /// <summary>The spawned battlefield, published once the map is generated and built.</summary>
    public sealed class BattleMap
    {
        public MapLayout Layout { get; }
        public IReadOnlyList<ReinforcementZoneComponent> ZoneViews { get; }
        public IReadOnlyList<CaptureSiteComponent> SiteViews { get; }
        public IReadOnlyList<MapObstacle> Obstacles { get; }
        public IReadOnlyList<StationObstacle> StationObstacles { get; }

        public BattleMap(
            MapLayout layout,
            IReadOnlyList<ReinforcementZoneComponent> zoneViews,
            IReadOnlyList<CaptureSiteComponent> siteViews,
            IReadOnlyList<MapObstacle> obstacles,
            IReadOnlyList<StationObstacle> stationObstacles)
        {
            Layout = layout;
            ZoneViews = zoneViews;
            SiteViews = siteViews;
            Obstacles = obstacles;
            StationObstacles = stationObstacles;
        }
    }
}
