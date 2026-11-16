using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Components.Ship.Health.HardPointOverlay
{
    /// <summary>Picks projected hardpoint icons without moving their screen positions.</summary>
    public static class HardPointMarkerLayout
    {
        public static int FindClosest(IReadOnlyList<Vector2> positions, Vector2 cursor, float halfSize)
        {
            int closest = -1;
            float distance = float.MaxValue;
            for (int index = 0; index < positions.Count; index++)
            {
                Vector2 delta = positions[index] - cursor;
                if (Mathf.Abs(delta.x) > halfSize || Mathf.Abs(delta.y) > halfSize ||
                    delta.sqrMagnitude >= distance) continue;
                closest = index;
                distance = delta.sqrMagnitude;
            }
            return closest;
        }

    }
}
