using System;
using UnityEngine;

namespace EmpireAtWar.Services.ShipNavigation
{
    internal sealed class ShipTrafficPath
    {
        public ShipTrafficPath(Vector3[] points)
        {
            if (points == null)
            {
                throw new ArgumentNullException(nameof(points));
            }

            if (points.Length < 2)
            {
                throw new ArgumentException(
                    "A traffic path requires at least two points.",
                    nameof(points));
            }

            Segments = new ShipTrafficSegment[points.Length - 1];
            Bounds = ShipTrafficBounds.FromPoints(points);
            for (int i = 0; i < Segments.Length; i++)
            {
                Segments[i] = new ShipTrafficSegment(points[i], points[i + 1]);
            }
        }

        public ShipTrafficSegment[] Segments { get; }
        public ShipTrafficBounds Bounds { get; }
    }

    internal readonly struct ShipTrafficSegment
    {
        public ShipTrafficSegment(Vector3 start, Vector3 end)
        {
            Start = start;
            End = end;
            Bounds = ShipTrafficBounds.FromSegment(start, end);
        }

        public Vector3 Start { get; }
        public Vector3 End { get; }
        public ShipTrafficBounds Bounds { get; }
    }

    internal readonly struct ShipTrafficBounds
    {
        private ShipTrafficBounds(
            float minimumX,
            float maximumX,
            float minimumZ,
            float maximumZ)
        {
            MinimumX = minimumX;
            MaximumX = maximumX;
            MinimumZ = minimumZ;
            MaximumZ = maximumZ;
        }

        public float MinimumX { get; }
        public float MaximumX { get; }
        public float MinimumZ { get; }
        public float MaximumZ { get; }

        public bool Overlaps(ShipTrafficBounds other, float clearance)
        {
            return MinimumX <= other.MaximumX + clearance &&
                   MaximumX + clearance >= other.MinimumX &&
                   MinimumZ <= other.MaximumZ + clearance &&
                   MaximumZ + clearance >= other.MinimumZ;
        }

        public static ShipTrafficBounds FromSegment(Vector3 start, Vector3 end)
        {
            return new ShipTrafficBounds(
                Mathf.Min(start.x, end.x),
                Mathf.Max(start.x, end.x),
                Mathf.Min(start.z, end.z),
                Mathf.Max(start.z, end.z));
        }

        public static ShipTrafficBounds FromPoints(Vector3[] points)
        {
            float minimumX = points[0].x;
            float maximumX = points[0].x;
            float minimumZ = points[0].z;
            float maximumZ = points[0].z;
            for (int i = 1; i < points.Length; i++)
            {
                Vector3 point = points[i];
                minimumX = Mathf.Min(minimumX, point.x);
                maximumX = Mathf.Max(maximumX, point.x);
                minimumZ = Mathf.Min(minimumZ, point.z);
                maximumZ = Mathf.Max(maximumZ, point.z);
            }

            return new ShipTrafficBounds(
                minimumX,
                maximumX,
                minimumZ,
                maximumZ);
        }
    }
}
