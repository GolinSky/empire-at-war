using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace EmpireAtWar.Services.ShipNavigation
{
    [BurstCompile]
    internal struct NavMeshEscapeJob : IJob
    {
        public float2 Origin;
        public float Clearance;
        [ReadOnly] public NativeArray<float3> Obstacles;
        [ReadOnly] public NativeArray<float2> Positions;
        [ReadOnly] public NativeArray<int> Triangles;
        [ReadOnly] public NativeArray<int> Regions;
        public NativeReference<NavMeshLocator.Result> Location;
        private double _best;
        private NavMeshLocator.Result _result;

        public void Execute()
        {
            _best = double.PositiveInfinity;
            _result = new NavMeshLocator.Result { Triangle = -1, Point = Origin };
            var tangents = new NativeList<double2>(Allocator.Temp);
            for (int i = 0; i < Obstacles.Length; i++)
            {
                float3 disk = Obstacles[i];
                double radius = disk.z + Clearance;
                double2 offset = (double2)Origin - disk.xy;
                if (math.lengthsq(offset) < radius * radius && math.any(offset != 0d))
                    tangents.Add(new double2(-offset.y, offset.x));
            }

            for (int id = 0; id < Regions.Length; id++)
            {
                if (Regions[id] == 0) continue;
                int first = 3 * id;
                float2 a = Positions[Triangles[first]];
                float2 b = Positions[Triangles[first + 1]];
                float2 c = Positions[Triangles[first + 2]];
                var triangle = new NavMeshTriangle(a, b, c);
                double2 closest = triangle.ClosestPoint(Origin);
                if (math.distancesq((double2)Origin, closest) >= _best) continue;
                Consider(id, triangle.RoundInside(closest));
                Consider(id, a);
                Consider(id, b);
                Consider(id, c);

                // An embedded disk permits the outward half-plane from the origin.
                // With overlapping disks the only legal directions can lie on its boundary.
                for (int i = 0; i < tangents.Length; i++)
                {
                    double2 direction = tangents[i];
                    if (!triangle.ClipLine(Origin, direction, out double enter, out double exit)) continue;
                    ConsiderIntersection(id, triangle, direction, enter, exit);
                    ConsiderIntersection(id, triangle, direction, exit, enter);
                }
            }
            tangents.Dispose();
            Location.Value = _result;
        }

        private void ConsiderIntersection(int id, NavMeshTriangle triangle, double2 direction, double edge, double otherEdge)
        {
            float2 candidate = (float2)((double2)Origin + direction * edge);
            if (!triangle.Contains(candidate))
            {
                // Keep the tangent direction when rounding into the triangle. Nudging
                // toward its centroid could move toward another overlapping disk.
                for (double t = 0.000001d; t < 1d && !triangle.Contains(candidate); t *= 8d)
                    candidate = (float2)((double2)Origin + direction * math.lerp(edge, otherEdge, t));
                if (!triangle.Contains(candidate))
                {
                    candidate = (float2)((double2)Origin + direction * ((edge + otherEdge) * 0.5d));
                    if (!triangle.Contains(candidate)) return;
                }
            }
            Consider(id, candidate);
        }

        private void Consider(int id, float2 candidate)
        {
            double distance = math.distancesq((double2)Origin, (double2)candidate);
            if (distance >= _best || !CanConnect(Origin, candidate, Obstacles, Clearance, true)) return;
            _best = distance;
            _result = new NavMeshLocator.Result { Triangle = id, Point = candidate };
        }

        public static bool CanConnect(float2 from, float2 to, NativeArray<float3> obstacles, float clearance, bool allowEscape)
        {
            if (from.Equals(to)) return true;
            double2 direction = (double2)to - from;
            double lengthSquared = math.lengthsq(direction);
            for (int i = 0; i < obstacles.Length; i++)
            {
                float3 disk = obstacles[i];
                double radius = disk.z + clearance;
                double2 offset = (double2)from - disk.xy;
                double dot = math.dot(offset, direction);
                if (allowEscape && math.lengthsq(offset) < radius * radius && dot >= 0d) continue;
                double t = math.saturate(-dot / lengthSquared);
                if (math.lengthsq(offset + t * direction) < radius * radius) return false;
            }
            return true;
        }
    }
}
