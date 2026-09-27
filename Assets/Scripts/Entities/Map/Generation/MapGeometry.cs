using System.Collections.Generic;
using EmpireAtWar.Models.SkirmishCamera;
using UnityEngine;

namespace EmpireAtWar.Entities.Map.Generation
{
    /// <summary>Ground-plane (XZ) helpers shared by the layout generation steps.</summary>
    public static class MapGeometry
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

        public static float Distance(Vector3 a, Vector3 b)
        {
            return new Vector2(a.x - b.x, a.z - b.z).magnitude;
        }

        public static float DistanceToPolyline(Vector3 point, IReadOnlyList<Vector3> polyline)
        {
            float closest = float.MaxValue;
            for (int i = 1; i < polyline.Count; i++)
            {
                closest = Mathf.Min(closest, DistanceToSegment(point, polyline[i - 1], polyline[i]));
            }

            return closest;
        }

        private static float DistanceToSegment(Vector3 point, Vector3 start, Vector3 end)
        {
            Vector2 p = new Vector2(point.x, point.z);
            Vector2 a = new Vector2(start.x, start.z);
            Vector2 ab = new Vector2(end.x, end.z) - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, Mathf.Epsilon));
            return (p - (a + ab * t)).magnitude;
        }
    }
}
