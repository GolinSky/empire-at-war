using EmpireAtWar.Utils;
using UnityEngine;

namespace EmpireAtWar.Entities.Map.Hazards
{
    /// <summary>One elliptical puff of a hazard cloud. <see cref="Length"/> is the semi-axis along
    /// <see cref="Direction"/> and is never shorter than <see cref="Width"/>.</summary>
    public readonly struct HazardLobe
    {
        public Vector3 Center { get; }
        public Vector3 Direction { get; }
        public float Length { get; }
        public float Width { get; }

        public HazardLobe(Vector3 center, Vector3 direction, float length, float width)
        {
            Center = center;
            Direction = direction;
            Length = length;
            Width = width;
        }

        /// <summary>Positive inside, zero on the ellipse; scaled by the width so it reads as metres near the sides.</summary>
        public float Evaluate(Vector3 position)
        {
            float x = position.x - Center.x;
            float z = position.z - Center.z;
            float along = (x * Direction.x + z * Direction.z) / Length;
            float across = (z * Direction.x - x * Direction.z) / Width;
            return Width * (1f - Mathf.Sqrt(along * along + across * across));
        }

        /// <summary>Distance to the capsule that encloses the ellipse; never more than the true distance.</summary>
        public float GetCapsuleDistance(Vector3 position)
        {
            Vector3 reach = Direction * (Length - Width);
            return Mathf.Sqrt(PlanarGeometry.DistanceToSegmentSquared(new Vector2(position.x, position.z),
                new Vector2(Center.x - reach.x, Center.z - reach.z),
                new Vector2(Center.x + reach.x, Center.z + reach.z))) - Width;
        }

        public float GetCapsuleDistance(HazardLobe other)
        {
            Vector3 reach = Direction * (Length - Width);
            Vector3 otherReach = other.Direction * (other.Length - other.Width);
            Vector2 a = new Vector2(Center.x - reach.x, Center.z - reach.z);
            Vector2 b = new Vector2(Center.x + reach.x, Center.z + reach.z);
            Vector2 c = new Vector2(other.Center.x - otherReach.x, other.Center.z - otherReach.z);
            Vector2 d = new Vector2(other.Center.x + otherReach.x, other.Center.z + otherReach.z);
            float distanceSquared = SegmentsCross(a, b, c, d) ? 0f : Mathf.Min(
                Mathf.Min(PlanarGeometry.DistanceToSegmentSquared(a, c, d), PlanarGeometry.DistanceToSegmentSquared(b, c, d)),
                Mathf.Min(PlanarGeometry.DistanceToSegmentSquared(c, a, b), PlanarGeometry.DistanceToSegmentSquared(d, a, b)));
            return Mathf.Sqrt(distanceSquared) - Width - other.Width;
        }

        public HazardLobe Mirror(Vector3 pivot)
        {
            return new HazardLobe(pivot * 2f - Center, -Direction, Length, Width);
        }

        private static bool SegmentsCross(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            return Cross(a, b, c) * Cross(a, b, d) < 0f && Cross(c, d, a) * Cross(c, d, b) < 0f;
        }

        private static float Cross(Vector2 a, Vector2 b, Vector2 point)
        {
            return (b.x - a.x) * (point.y - a.y) - (b.y - a.y) * (point.x - a.x);
        }
    }
}
