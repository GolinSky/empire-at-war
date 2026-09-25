using UnityEngine;

namespace EmpireAtWar.Models.MiniMap
{
    public interface IMiniMapObstacleSource
    {
        Bounds WorldBounds { get; }
    }
}
