using Unity.Mathematics;

namespace EmpireAtWar.Services.ShipNavigation
{
    // BurstTriangulator emits clockwise triangles. Double intermediates keep long, thin
    // triangles and tangent disk edges classifiable at battle-map coordinate scales.
    internal readonly struct NavMeshTriangle
    {
        private readonly double2 _a;
        private readonly double2 _b;
        private readonly double2 _c;

        public NavMeshTriangle(float2 a, float2 b, float2 c)
        {
            _a = a;
            _b = b;
            _c = c;
        }

        public bool Contains(double2 point) =>
            Cross(_b - _a, point - _a) <= 0d &&
            Cross(_c - _b, point - _b) <= 0d &&
            Cross(_a - _c, point - _c) <= 0d;

        public double2 ClosestPoint(double2 point)
        {
            if (Contains(point)) return point;
            double2 closest = OnEdge(_a, _b, point);
            double2 bc = OnEdge(_b, _c, point);
            double2 ca = OnEdge(_c, _a, point);
            if (math.distancesq(bc, point) < math.distancesq(closest, point)) closest = bc;
            if (math.distancesq(ca, point) < math.distancesq(closest, point)) closest = ca;
            return closest;
        }

        public float2 RoundInside(double2 point)
        {
            float2 rounded = (float2)point;
            if (Contains(rounded)) return rounded;
            // A rounded edge projection can fall outside. Move toward the centroid
            // until the point is representable on this triangle's side of the edge.
            float2 center = (float2)((_a + _b + _c) / 3d);
            for (float t = 0.000001f; t < 1f; t *= 8f)
            {
                float2 inside = math.lerp(rounded, center, t);
                if (Contains(inside)) return inside;
            }
            if (Contains(center)) return center;
            double2 nearestVertex = math.distancesq(point, _a) <= math.distancesq(point, _b) ? _a : _b;
            return (float2)(math.distancesq(point, _c) < math.distancesq(point, nearestVertex) ? _c : nearestVertex);
        }

        public bool ClipLine(double2 origin, double2 direction, out double enter, out double exit)
        {
            enter = double.NegativeInfinity;
            exit = double.PositiveInfinity;
            return ClipEdge(_a, _b, origin, direction, ref enter, ref exit) &&
                   ClipEdge(_b, _c, origin, direction, ref enter, ref exit) &&
                   ClipEdge(_c, _a, origin, direction, ref enter, ref exit);
        }

        private static bool ClipEdge(double2 a, double2 b, double2 origin, double2 direction,
            ref double enter, ref double exit)
        {
            // The clockwise interior satisfies cross(edge, origin + t * direction - a) <= 0.
            double slope = Cross(b - a, direction);
            double side = Cross(b - a, origin - a);
            if (slope == 0d) return side <= 0d;
            double time = -side / slope;
            if (slope < 0d) enter = math.max(enter, time);
            else exit = math.min(exit, time);
            return enter <= exit;
        }

        private static double2 OnEdge(double2 a, double2 b, double2 point)
        {
            double2 edge = b - a;
            return a + edge * math.saturate(math.dot(point - a, edge) / math.lengthsq(edge));
        }

        private static double Cross(double2 a, double2 b) => a.x * b.y - a.y * b.x;
    }
}
