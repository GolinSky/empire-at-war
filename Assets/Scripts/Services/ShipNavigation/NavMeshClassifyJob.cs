using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace EmpireAtWar.Services.ShipNavigation
{
    [BurstCompile]
    internal struct NavMeshClassifyJob : IJob
    {
        public NavMeshBuckets Layout;
        [ReadOnly] public NativeArray<float3> Disks;
        [ReadOnly] public NativeParallelMultiHashMap<int, int> DiskBuckets;
        [ReadOnly] public NativeArray<float2> Positions;
        [ReadOnly] public NativeArray<int> Triangles;
        public NativeArray<int> Regions;
        public NativeParallelMultiHashMap<int, int> TriangleBuckets;

        public void Execute()
        {
            var stamps = new NativeArray<int>(Disks.Length, Allocator.Temp);
            for (int triangle = 0; triangle < Regions.Length; triangle++)
            {
                int first = triangle * 3;
                float2 a = Positions[Triangles[first]];
                float2 b = Positions[Triangles[first + 1]];
                float2 c = Positions[Triangles[first + 2]];
                float2 min = math.min(a, math.min(b, c));
                float2 max = math.max(a, math.max(b, c));
                bool blocked = IntersectsDisk(new NavMeshTriangle(a, b, c), min, max, stamps, triangle + 1);
                Regions[triangle] = blocked ? 0 : NavMeshRegionJob.UNLABELED;
                if (!blocked) Layout.Insert(TriangleBuckets, triangle, min, max);
            }
            stamps.Dispose();
        }

        private bool IntersectsDisk(NavMeshTriangle triangle, float2 min, float2 max, NativeArray<int> stamps, int stamp)
        {
            int2 first = Layout.Cell(min);
            int2 last = Layout.Cell(max);
            for (int y = first.y; y <= last.y; y++)
            for (int x = first.x; x <= last.x; x++)
            {
                foreach (int index in DiskBuckets.GetValuesForKey(NavMeshBuckets.Key(x, y)))
                {
                    if (stamps[index] == stamp) continue;
                    stamps[index] = stamp;
                    float3 disk = Disks[index];
                    double2 center = disk.xy;
                    if (math.distancesq(triangle.ClosestPoint(center), center) < (double)disk.z * disk.z)
                        return true;
                }
            }
            return false;
        }
    }
}
