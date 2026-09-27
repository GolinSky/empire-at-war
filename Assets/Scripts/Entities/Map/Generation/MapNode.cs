using EmpireAtWar.Models.Factions;
using UnityEngine;

namespace EmpireAtWar.Entities.Map.Generation
{
    /// <summary>A point of interest that roads connect; every node except default zones is walled off.</summary>
    public sealed class MapNode
    {
        public MapNode(MapNodeKind kind, Vector3 center, float radius, PlayerType owner)
        {
            Kind = kind;
            Center = center;
            Radius = radius;
            Owner = owner;
            Mirror = -1;
        }

        public MapNodeKind Kind { get; }
        public Vector3 Center { get; }
        public float Radius { get; }
        public PlayerType Owner { get; }
        public bool IsWalled => Kind != MapNodeKind.DefaultZone;
        /// <summary>Index of the node that point symmetry pairs with this one; its own index at the map center.</summary>
        public int Mirror { get; set; }
    }
}
