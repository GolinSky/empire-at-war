using UnityEngine;

namespace EmpireAtWar.Components.Ship.Movement
{
    public readonly struct ShipRoutePiece
    {
        public Vector3 Start { get; }
        public float Heading { get; }
        public float Length { get; }
        // Positive curvature increases Unity yaw: a right turn in the XZ plane.
        public float Curvature { get; }
        public Vector3 InitialTangent => new Vector3(Mathf.Sin(Heading), 0f, Mathf.Cos(Heading));
        public Vector3 End => Evaluate(Length, out _);
        public Vector3 EndTangent => new Vector3(
            Mathf.Sin(Heading + Length * Curvature), 0f, Mathf.Cos(Heading + Length * Curvature));

        public ShipRoutePiece(Vector3 start, float heading, float length, float curvature)
        {
            Start = start;
            Heading = heading;
            Length = length;
            Curvature = curvature;
        }

        public static ShipRoutePiece Line(Vector3 start, Vector3 end)
        {
            Vector3 direction = end - start;
            return new ShipRoutePiece(start, Mathf.Atan2(direction.x, direction.z),
                new Vector2(direction.x, direction.z).magnitude, 0f);
        }

        public static ShipRoutePiece Arc(Vector3 start, Vector3 tangent, float length, float curvature) =>
            new ShipRoutePiece(start, Mathf.Atan2(tangent.x, tangent.z), length, curvature);

        public Vector3 Evaluate(float distance, out Vector3 tangent)
        {
            float travelled = Mathf.Clamp(distance, 0f, Length);
            float angle = Heading + travelled * Curvature;
            tangent = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
            if (Curvature == 0f) return Start + tangent * travelled;
            // Midpoint form avoids subtracting nearly equal sine/cosine values on short arcs.
            float halfSweep = travelled * Curvature * 0.5f;
            float chord = 2f * Mathf.Sin(halfSweep) / Curvature;
            float middle = Heading + halfSweep;
            return Start + new Vector3(Mathf.Sin(middle), 0f, Mathf.Cos(middle)) * chord;
        }
    }
}
