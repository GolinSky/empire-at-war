using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Entities.Map
{
    /// <summary>An invisible route kept free of asteroid fields between two points of interest.</summary>
    public sealed class MapLane
    {
        public MapLane(IReadOnlyList<Vector3> points)
        {
            Points = points;
        }

        public IReadOnlyList<Vector3> Points { get; }
    }
}
