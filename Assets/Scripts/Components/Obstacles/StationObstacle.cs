using EmpireAtWar.Components.Radar;
using EmpireAtWar.Services.ShipNavigation;
using UnityEngine;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Components.Obstacles
{
    /// <summary>
    /// Static navigation obstacle over a space station footprint. Radar reports stations as unit
    /// contacts, which navigation ignores, so ships would otherwise fly through them.
    /// </summary>
    public sealed class StationObstacle : IMapObstacleContactSource
    {
        public RadarContact Contact { get; }
        public PlayerId Owner { get; }

        public StationObstacle(PlayerId owner, Vector3 position, float radius)
        {
            Owner = owner;
            // The footprint radius doubles as the vertical reach: ships far below pass under.
            Contact = new RadarContact(position, radius, false,
                position.y - radius, position.y + radius);
        }
    }
}
