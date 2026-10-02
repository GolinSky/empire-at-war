using UnityEngine;

namespace EmpireAtWar.Utils
{
    /// <summary>Map-plane (XZ) geometry. Gameplay ranges ignore height so ship height tiers
    /// never change who can see or hit whom.</summary>
    public static class PlanarGeometry
    {
        public static float Distance(Vector3 first, Vector3 second) =>
            Mathf.Sqrt(DistanceSquared(first, second));

        public static float DistanceSquared(Vector3 first, Vector3 second)
        {
            float deltaX = first.x - second.x;
            float deltaZ = first.z - second.z;
            return deltaX * deltaX + deltaZ * deltaZ;
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
