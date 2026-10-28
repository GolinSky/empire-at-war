using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Entities.Map
{
    public readonly struct SiteSpot
    {
        public Vector3 Center { get; }
        public CaptureSiteKind Kind { get; }
        public PlayerId Owner { get; }
        public int PointId { get; }

        public SiteSpot(Vector3 center, CaptureSiteKind kind, PlayerId owner = default, int pointId = -1)
        {
            Center = center;
            Kind = kind;
            Owner = owner;
            PointId = pointId;
        }
    }
}
