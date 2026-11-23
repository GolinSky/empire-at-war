using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>Own units a new AI ship may spawn beside, best first.</summary>
    public interface IEnemyForwardSpawnAnchors
    {
        /// <param name="landedShips">Ships placed earlier in this tick, not yet in the world snapshot.</param>
        /// <param name="capital">The ship is an assault capital: it needs an escorted anchor.</param>
        bool Collect(Vector3 task, IReadOnlyList<Vector3> landedShips, bool capital,
            List<ForwardSpawnAnchor> anchors, out Vector3 awayFrom);

        /// <summary>
        /// Anchors for a planned attack wave dropped beside the task (W68): the deep and capital escort filters are
        /// relaxed to the human spawn rule (<c>ReinforcementSpawnRule.CanSpawnShip</c>, checked when the point is
        /// picked); the known-hostile clearance stays. Normal drops keep using <see cref="Collect"/>.
        /// </summary>
        /// <param name="landedShips">Ships placed earlier in this tick, not yet in the world snapshot.</param>
        bool CollectPlannedWave(Vector3 task, IReadOnlyList<Vector3> landedShips,
            List<ForwardSpawnAnchor> anchors, out Vector3 awayFrom);
    }
}
