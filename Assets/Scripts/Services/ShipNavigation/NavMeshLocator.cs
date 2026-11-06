using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace EmpireAtWar.Services.ShipNavigation
{
    [BurstCompile]
    internal struct NavMeshLocator : IJob
    {
        public struct Result
        {
            public int Triangle;
            public float2 Point;
        }

        public NavMeshBuckets Layout;
        [ReadOnly] public NativeParallelMultiHashMap<int, int> Buckets;
        [ReadOnly] public NativeArray<int> Regions;
        [ReadOnly] public NativeArray<float2> Positions;
        [ReadOnly] public NativeArray<int> Triangles;
        public float2 Point;
        public int Region;
        public int PreferredRegion;
        public NativeReference<Result> Location;

        public void Execute()
        {
            var result = new Result { Triangle = -1, Point = Point };
            if (math.all(Point >= Layout.Min) && math.all(Point <= Layout.Max))
            {
                int2 cell = Layout.Cell(Point);
                foreach (int id in Buckets.GetValuesForKey(NavMeshBuckets.Key(cell.x, cell.y)))
                {
                    if (Region != 0 && Regions[id] != Region) continue;
                    if (WinsTie(id, result.Triangle) && Triangle(id).Contains(Point))
                        result.Triangle = id;
                }
            }

            if (result.Triangle < 0)
            {
                double best = double.PositiveInfinity;
                for (int id = 0; id < Regions.Length; id++)
                {
                    if (Regions[id] == 0 || (Region != 0 && Regions[id] != Region)) continue;
                    NavMeshTriangle triangle = Triangle(id);
                    double2 projected = triangle.ClosestPoint(Point);
                    double distance = math.distancesq(projected, (double2)Point);
                    if (distance > best || (distance == best && !WinsTie(id, result.Triangle))) continue;
                    best = distance;
                    result.Triangle = id;
                    result.Point = (float2)projected;
                }

                if (result.Triangle >= 0)
                {
                    result.Point = Triangle(result.Triangle).RoundInside(result.Point);
                }
            }
            Location.Value = result;
        }

        private bool WinsTie(int candidate, int current)
        {
            if (current < 0) return true;
            bool candidatePreferred = Regions[candidate] == PreferredRegion;
            bool currentPreferred = Regions[current] == PreferredRegion;
            return candidatePreferred == currentPreferred ? candidate < current : candidatePreferred;
        }

        private NavMeshTriangle Triangle(int id)
        {
            int edge = id * 3;
            return new NavMeshTriangle(Positions[Triangles[edge]], Positions[Triangles[edge + 1]],
                Positions[Triangles[edge + 2]]);
        }
    }
}
