using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Entities.Map
{
    public sealed class MapRoad
    {
        public MapRoad(IReadOnlyList<Vector3> points)
        {
            Points = points;
        }

        public IReadOnlyList<Vector3> Points { get; }
    }
}
