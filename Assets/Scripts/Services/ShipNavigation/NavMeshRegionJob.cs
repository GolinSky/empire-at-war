using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace EmpireAtWar.Services.ShipNavigation
{
    [BurstCompile]
    internal struct NavMeshRegionJob : IJob
    {
        public const int UNLABELED = -1;
        [ReadOnly] public NativeArray<int> Halfedges;
        public NativeArray<int> Regions;

        public void Execute()
        {
            var queue = new NativeArray<int>(Regions.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            int label = 0;
            for (int seed = 0; seed < Regions.Length; seed++)
            {
                if (Regions[seed] != UNLABELED) continue;
                label++;
                Regions[seed] = label;
                queue[0] = seed;
                int tail = 1;
                for (int head = 0; head < tail; head++)
                {
                    int first = 3 * queue[head];
                    for (int edge = first; edge < first + 3; edge++)
                    {
                        int opposite = Halfedges[edge];
                        if (opposite < 0) continue;
                        int neighbor = opposite / 3;
                        if (Regions[neighbor] != UNLABELED) continue;
                        Regions[neighbor] = label;
                        queue[tail++] = neighbor;
                    }
                }
            }
            queue.Dispose();
        }
    }
}
