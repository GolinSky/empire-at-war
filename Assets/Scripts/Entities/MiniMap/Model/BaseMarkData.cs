using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Models.MiniMap
{
    /// <summary>A space station on the minimap, colored by how its owner relates to the local player.</summary>
    public sealed class BaseMarkData : MarkData
    {
        public BaseMarkData(Vector3 position, Sprite icon, OwnerRelation relation) : base(position, icon)
        {
            Relation = relation;
        }

        public OwnerRelation Relation { get; }
    }
}
