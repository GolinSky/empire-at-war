using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.Services.Vision
{
    public static class VisionHullGeometry
    {
        public static bool IntersectsCircle(IReadOnlyList<Vector2> hull, Vector2 center, float radius)
        {
            bool inside = false;
            float radiusSquared = radius * radius;
            Vector2 previous = hull[hull.Count - 1];
            for (int i = 0; i < hull.Count; i++)
            {
                Vector2 current = hull[i];
                Vector2 edge = current - previous;
                float lengthSquared = edge.sqrMagnitude;
                float fraction = lengthSquared > 0f
                    ? Mathf.Clamp01(Vector2.Dot(center - previous, edge) / lengthSquared)
                    : 0f;
                if ((center - (previous + edge * fraction)).sqrMagnitude <= radiusSquared) return true;

                if ((previous.y > center.y) != (current.y > center.y) &&
                    center.x < previous.x + (center.y - previous.y) * edge.x / edge.y)
                    inside = !inside;
                previous = current;
            }

            return inside;
        }
    }
}
