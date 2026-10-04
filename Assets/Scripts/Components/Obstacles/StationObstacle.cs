using EmpireAtWar.Components.Radar;
using EmpireAtWar.Services.ShipNavigation;
using UnityEngine;

namespace EmpireAtWar.Components.Obstacles
{
    /// <summary>
    /// Static navigation obstacle over a space station footprint. Radar reports stations as unit
    /// contacts, which navigation ignores, so ships would otherwise fly through them.
    /// </summary>
    public sealed class StationObstacle : IMapObstacleContactSource
    {
        public RadarContact Contact { get; }

        public StationObstacle(Vector3 position, float radius)
        {
            // The footprint radius doubles as the vertical reach: ships far below pass under.
            Contact = new RadarContact(position, radius, false,
                position.y - radius, position.y + radius);
        }
    }
}
