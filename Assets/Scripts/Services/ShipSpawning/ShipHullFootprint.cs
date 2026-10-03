using UnityEngine;

namespace EmpireAtWar.Services.ShipSpawning
{
    /// <summary>A ship hull box in world space. Ships only yaw, so overlap is a rotated rectangle
    /// test on the XZ plane plus a vertical span test.</summary>
    public readonly struct ShipHullFootprint
    {
        public ShipHullFootprint(Vector3 center, Vector3 halfExtents, Quaternion rotation)
        {
            Center = center;
            HalfExtents = halfExtents;
            Rotation = rotation;
        }

        public Vector3 Center { get; }
        public Vector3 HalfExtents { get; }
        public Quaternion Rotation { get; }

        public bool Overlaps(ShipHullFootprint other)
        {
            if (Mathf.Abs(Center.y - other.Center.y) >= HalfExtents.y + other.HalfExtents.y)
            {
                return false;
            }

            // Separating axis test: the rectangles overlap only if no edge normal separates them.
            return !IsSeparated(other, PlanarAxis(Rotation * Vector3.right)) &&
                   !IsSeparated(other, PlanarAxis(Rotation * Vector3.forward)) &&
                   !IsSeparated(other, PlanarAxis(other.Rotation * Vector3.right)) &&
                   !IsSeparated(other, PlanarAxis(other.Rotation * Vector3.forward));
        }

        private bool IsSeparated(ShipHullFootprint other, Vector2 axis)
        {
            Vector2 offset = new Vector2(other.Center.x - Center.x, other.Center.z - Center.z);
            return Mathf.Abs(Vector2.Dot(offset, axis)) >= ProjectedExtent(axis) + other.ProjectedExtent(axis);
        }

        private float ProjectedExtent(Vector2 axis)
        {
            return HalfExtents.x * Mathf.Abs(Vector2.Dot(PlanarAxis(Rotation * Vector3.right), axis)) +
                   HalfExtents.z * Mathf.Abs(Vector2.Dot(PlanarAxis(Rotation * Vector3.forward), axis));
        }

        private static Vector2 PlanarAxis(Vector3 direction) => new Vector2(direction.x, direction.z).normalized;
    }
}
