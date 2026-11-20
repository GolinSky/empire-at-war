using System.Collections.Generic;
using EmpireAtWar.Services.Enemy;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    /// <summary>No own unit to spawn beside: forward deployment falls back to the relay or station search.</summary>
    public sealed class NoForwardSpawnAnchors : IEnemyForwardSpawnAnchors
    {
        public static readonly NoForwardSpawnAnchors Instance = new NoForwardSpawnAnchors();

        public bool Collect(Vector3 task, IReadOnlyList<Vector3> landedShips, List<ForwardSpawnAnchor> anchors,
            out Vector3 awayFrom)
        {
            anchors.Clear();
            awayFrom = default;
            return false;
        }
    }
}
