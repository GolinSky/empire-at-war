using System.Collections.Generic;
using EmpireAtWar.Models.SkirmishCamera;
using UnityEngine;

namespace EmpireAtWar.Utils
{
    /// <summary>Map-plane (XZ) geometry. Gameplay ranges ignore height so ship height tiers
    /// never change who can see or hit whom.</summary>
    public static class PlanarGeometry
    {
        public static Vector3 GetCenter(Vector2Range bounds)
        {
            Vector2 center = (bounds.Min + bounds.Max) * 0.5f;
            return new Vector3(center.x, 0f, center.y);
        }

        public static Vector3 Reflect(Vector3 position, Vector3 center)
        {
            return new Vector3(2f * center.x - position.x, position.y, 2f * center.z - position.z);
        }

        public static Vector3 NormalizeDirection(Vector3 direction, Vector3 fallback)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude > Mathf.Epsilon)
            {
                return direction.normalized;
            }

            fallback.y = 0f;
            return fallback.sqrMagnitude > Mathf.Epsilon
                ? fallback.normalized
                : Vector3.forward;
        }

        public static float Distance(Vector3 first, Vector3 second) =>
            Mathf.Sqrt(DistanceSquared(first, second));

        public static float DistanceSquared(Vector3 first, Vector3 second)
        {
            float deltaX = first.x - second.x;
            float deltaZ = first.z - second.z;
            return deltaX * deltaX + deltaZ * deltaZ;
        }

        public static bool ContainsCircle(Vector3 point, Vector3 center, float radius) =>
            DistanceSquared(point, center) <= radius * radius;

        public static float DistanceToPolyline(Vector3 point, IReadOnlyList<Vector3> polyline)
        {
            float closest = float.MaxValue;
            for (int i = 1; i < polyline.Count; i++)
            {
                closest = Mathf.Min(closest, Mathf.Sqrt(DistanceToSegmentSquared(
                    new Vector2(point.x, point.z),
                    new Vector2(polyline[i - 1].x, polyline[i - 1].z),
                    new Vector2(polyline[i].x, polyline[i].z))));
            }

            return closest;
        }

        public static float DistanceToSegmentSquared(Vector2 point, Vector2 start, Vector2 end)
        {
            Vector2 segment = end - start;
            float lengthSquared = segment.sqrMagnitude;
            if (lengthSquared <= Mathf.Epsilon) return (point - start).sqrMagnitude;
            float parameter = Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared);
            return (point - (start + segment * parameter)).sqrMagnitude;
        }
    }
}
