using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Entities.Map.Generation
{
    /// <summary>A point of interest that lanes connect; every node except default zones gets an asteroid pocket.</summary>
    public sealed class MapNode
    {
        public MapNodeKind Kind { get; }
        public Vector3 Center { get; }
        public float Radius { get; }
        public PlayerId Owner { get; }
        public int PointId { get; }
        public bool HasPocket => Kind != MapNodeKind.DefaultZone && Kind != MapNodeKind.SatelliteSite;
        /// <summary>Index of the paired node; asymmetric main-zone nodes and unpaired homes use their own index.</summary>
        public int Mirror { get; set; }

        public MapNode(MapNodeKind kind, Vector3 center, PlayerId owner, float radius, int pointId = -1)
        {
            Kind = kind;
            Center = center;
            Radius = radius;
            Owner = owner;
            PointId = pointId;
            Mirror = -1;
        }
    }
}
