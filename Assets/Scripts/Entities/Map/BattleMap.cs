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
        public IReadOnlyList<ReinforcementZoneView> ZoneViews { get; }
        public IReadOnlyList<CaptureSiteView> SiteViews { get; }
        public IReadOnlyList<MapObstacle> Obstacles { get; }
        public IReadOnlyList<StationObstacle> StationObstacles { get; }

        public BattleMap(
            MapLayout layout,
            IReadOnlyList<ReinforcementZoneView> zoneViews,
            IReadOnlyList<CaptureSiteView> siteViews,
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
