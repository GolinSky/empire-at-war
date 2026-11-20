using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>Own units a new AI ship may spawn beside, best first.</summary>
    public interface IEnemyForwardSpawnAnchors
    {
        /// <param name="landedShips">Ships placed earlier in this tick, not yet in the world snapshot.</param>
        bool Collect(Vector3 task, IReadOnlyList<Vector3> landedShips, List<ForwardSpawnAnchor> anchors,
            out Vector3 awayFrom);
    }
}
