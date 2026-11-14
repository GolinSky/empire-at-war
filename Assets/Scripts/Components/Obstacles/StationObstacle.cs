using EmpireAtWar.Components.Radar;
using UnityEngine;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Components.Obstacles
{
    /// <summary>
    /// Static navigation obstacle over a space station footprint. Radar reports stations as unit
    /// contacts, which navigation ignores, so ships would otherwise fly through them.
    /// </summary>
    public sealed class StationObstacle
    {
        public Vector3 Position { get; }
        public float Radius { get; }
        public PlayerId Owner { get; }

        public StationObstacle(PlayerId owner, Vector3 position, float radius)
        {
            Owner = owner;
            Position = position;
            Radius = radius;
        }

        public RadarContact GetContact(float bottom, float top) =>
            new RadarContact(Position, Radius, false, bottom, top);
    }
}
