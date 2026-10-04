using UnityEngine;

namespace EmpireAtWar.Components.Radar
{
    public readonly struct RadarContact
    {
        public Vector3 Position { get; }
        public float Radius { get; }
        public bool IsShip { get; }
        /// <summary>World-space vertical extent. Unbounded unless the source knows its height.</summary>
        public float Bottom { get; }
        public float Top { get; }

        public RadarContact(Vector3 position, float radius, bool isShip)
            : this(position, radius, isShip, float.NegativeInfinity, float.PositiveInfinity)
        {
        }

        public RadarContact(Vector3 position, float radius, bool isShip, float bottom, float top)
        {
            Position = position;
            Radius = radius;
            IsShip = isShip;
            Bottom = bottom;
            Top = top;
        }

        public static RadarContact FromBounds(Bounds bounds, bool isShip)
        {
            return new RadarContact(
                bounds.center,
                Mathf.Max(bounds.extents.x, bounds.extents.z),
                isShip,
                bounds.min.y,
                bounds.max.y);
        }
    }
}
