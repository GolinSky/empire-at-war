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
        public bool HasPocket => Kind != MapNodeKind.DefaultZone;
        /// <summary>Index of the node that point symmetry pairs with this one; its own index at the map center.</summary>
        public int Mirror { get; set; }

        public MapNode(MapNodeKind kind, Vector3 center, PlayerId owner, float radius)
        {
            Kind = kind;
            Center = center;
            Radius = radius;
            Owner = owner;
            Mirror = -1;
        }
    }
}
