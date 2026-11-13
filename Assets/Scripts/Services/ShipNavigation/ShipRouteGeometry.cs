using EmpireAtWar.Components.Ship.Movement;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace EmpireAtWar.Services.ShipNavigation
{
    internal static class ShipRouteGeometry
    {
        public const float EPSILON = 0.0001f;

        public static float2 Right(float2 direction) => new float2(direction.y, -direction.x);
        public static float Cross(float2 a, float2 b) => a.x * b.y - a.y * b.x;
        public static float2 Planar(Vector3 value) => new float2(value.x, value.z);
        public static Vector3 World(float2 value, float height) => new Vector3(value.x, height, value.y);

        public static bool Tangent(ShipRouteCircle a, ShipRouteCircle b, out float2 from, out float2 to)
        {
            float2 delta = b.Center - a.Center;
            float distance = math.length(delta);
            float offset = a.Sign * a.Radius - b.Sign * b.Radius;
            // Circles touching within EPSILON keep a degenerate tangent at the contact point:
            // consecutive corner fillets capped at half their shared leg touch exactly.
            if (distance <= EPSILON || distance < math.abs(offset) - EPSILON)
            {
                from = a.Center;
                to = b.Center;
                return false;
            }
            float2 direction = delta / distance;
            float ratio = math.clamp(offset / distance, -1f, 1f);
            float2 normal = -Right(direction) * math.sqrt(math.max(0f, 1f - ratio * ratio)) + direction * ratio;
            from = a.Center + normal * (a.Sign * a.Radius);
            to = b.Center + normal * (b.Sign * b.Radius);
            return true;
        }

        public static float Sweep(float2 from, float2 to, int sign)
        {
            return math.atan2(-Cross(from, to), math.dot(from, to)) * sign;
        }

        public static ShipRoutePiece Line(float2 from, float2 to, float height)
        {
            float2 delta = to - from;
            return new ShipRoutePiece(World(from, height), math.atan2(delta.x, delta.y), math.length(delta), 0f);
        }

        public static ShipRoutePiece Arc(ShipRouteCircle circle, float2 from, float sweep, float height)
        {
            float2 tangent = Right((from - circle.Center) / circle.Radius) * circle.Sign;
            return new ShipRoutePiece(World(from, height), math.atan2(tangent.x, tangent.y),
                sweep * circle.Radius, circle.Sign / circle.Radius);
        }

        public static float2 Point(ShipRoutePiece piece, float distance)
        {
            float2 start = Planar(piece.Start);
            float half = distance * piece.Curvature * 0.5f;
            float chord = piece.Curvature == 0f ? distance : 2f * math.sin(half) / piece.Curvature;
            return start + chord * new float2(math.sin(piece.Heading + half), math.cos(piece.Heading + half));
        }

        public static bool OnArc(float2 radial, float2 startRadial, float sweep, int sign)
        {
            float angle = Sweep(startRadial, radial, sign);
            if (angle < -EPSILON) angle += 2f * math.PI;
            return angle <= sweep + EPSILON;
        }

        public static void Bounds(ShipRoutePiece piece, out float2 min, out float2 max)
        {
            float2 start = Planar(piece.Start);
            float2 end = Point(piece, piece.Length);
            min = math.min(start, end);
            max = math.max(start, end);
            if (piece.Curvature == 0f) return;
            float radius = math.abs(1f / piece.Curvature);
            int sign = piece.Curvature > 0f ? 1 : -1;
            float2 center = start + Right(new float2(math.sin(piece.Heading), math.cos(piece.Heading))) / piece.Curvature;
            float sweep = piece.Length / radius;
            for (int i = 0; i < 4; i++)
            {
                float2 radial = i == 0 ? new float2(1, 0) : i == 1 ? new float2(0, 1) :
                    i == 2 ? new float2(-1, 0) : new float2(0, -1);
                if (!OnArc(radial, start - center, sweep, sign)) continue;
                min = math.min(min, center + radial * radius);
                max = math.max(max, center + radial * radius);
            }
        }

        public static float DistanceSquared(ShipRoutePiece piece, float2 point)
        {
            float2 start = Planar(piece.Start);
            float2 end = Point(piece, piece.Length);
            if (piece.Curvature == 0f)
            {
                float2 delta = end - start;
                float t = math.saturate(math.dot(point - start, delta) / math.max(math.lengthsq(delta), EPSILON * EPSILON));
                return math.distancesq(point, start + t * delta);
            }
            float2 center = start + Right(new float2(math.sin(piece.Heading), math.cos(piece.Heading))) / piece.Curvature;
            float radius = math.abs(1f / piece.Curvature);
            float2 radial = point - center;
            if (OnArc(radial, start - center, piece.Length / radius, piece.Curvature > 0f ? 1 : -1))
            {
                float difference = math.length(radial) - radius;
                return difference * difference;
            }
            return math.min(math.distancesq(point, start), math.distancesq(point, end));
        }

        public static int FindBlocker(ShipRoutePiece piece, NativeArray<float3> disks,
            NativeParallelMultiHashMap<int, int> buckets, NavMeshBuckets layout,
            int skipA = -1, int skipB = -1, bool allowEscape = false)
        {
            Bounds(piece, out float2 min, out float2 max);
            int2 first = layout.Cell(min);
            int2 last = layout.Cell(max);
            int blocker = -1;
            float nearest = float.MaxValue;
            for (int y = first.y; y <= last.y; y++)
            for (int x = first.x; x <= last.x; x++)
            foreach (int index in buckets.GetValuesForKey(NavMeshBuckets.Key(x, y)))
            {
                if (index == skipA || index == skipB) continue;
                float3 disk = disks[index];
                float radius = math.max(0f, disk.z - math.max(EPSILON, disk.z * 0.00001f));
                if (DistanceSquared(piece, disk.xy) >= radius * radius) continue;
                float distance = math.distancesq(Planar(piece.Start), disk.xy);
                if (allowEscape && distance < disk.z * disk.z && MovesAway(piece, disk.xy)) continue;
                if (distance >= nearest) continue;
                nearest = distance;
                blocker = index;
            }
            return blocker;
        }

        private static bool MovesAway(ShipRoutePiece piece, float2 obstacle)
        {
            float2 start = Planar(piece.Start);
            float2 tangent = new float2(math.sin(piece.Heading), math.cos(piece.Heading));
            float outward = math.dot(start - obstacle, tangent);
            if (outward < 0f) return false;
            if (piece.Curvature == 0f || piece.Length == 0f) return true;

            // The arc stops moving away once its radius points directly away from the obstacle.
            float2 radial = Right(tangent) * (piece.Curvature > 0f ? -1f : 1f);
            float2 away = start - obstacle - radial / math.abs(piece.Curvature);
            if (math.lengthsq(away) == 0f) return true;
            float sweep = math.atan2(outward, math.dot(radial, away));
            return piece.Length * math.abs(piece.Curvature) <= sweep;
        }

        public static bool InsideBounds(ShipRoutePiece piece, NavMeshBuckets layout)
        {
            Bounds(piece, out float2 min, out float2 max);
            return math.all(min >= layout.Min - EPSILON) && math.all(max <= layout.Max + EPSILON);
        }
    }
}
