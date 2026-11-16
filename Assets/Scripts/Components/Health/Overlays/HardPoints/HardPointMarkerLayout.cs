using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Components.Ship.Health.HardPointOverlay
{
    /// <summary>Separates screen-space markers in stable hardpoint order, including coincident anchors.</summary>
    public static class HardPointMarkerLayout
    {
        public static void Separate(List<Vector2> positions, float spacing)
        {
            for (int index = 1; index < positions.Count; index++)
            {
                Vector2 anchor = positions[index];
                if (IsFree(positions, index, anchor, spacing)) continue;

                // Search successive square perimeters. Earlier markers never move, so placement is deterministic.
                bool placed = false;
                for (int radius = 1; !placed; radius++)
                {
                    float nearest = float.MaxValue;
                    for (int x = -radius; x <= radius; x++)
                    {
                        for (int y = -radius; y <= radius; y++)
                        {
                            if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius) continue;
                            float distance = x * x + y * y;
                            if (distance >= nearest) continue;
                            Vector2 candidate = anchor + new Vector2(x, y) * spacing;
                            if (!IsFree(positions, index, candidate, spacing)) continue;
                            positions[index] = candidate;
                            nearest = distance;
                            placed = true;
                        }
                    }
                }
            }
        }

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

        private static bool IsFree(IReadOnlyList<Vector2> positions, int count, Vector2 candidate, float spacing)
        {
            for (int index = 0; index < count; index++)
            {
                Vector2 delta = candidate - positions[index];
                if (Mathf.Abs(delta.x) < spacing && Mathf.Abs(delta.y) < spacing) return false;
            }
            return true;
        }
    }
}
