using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace EmpireAtWar.Services.ShipNavigation
{
    [BurstCompile]
    internal struct NavigationGridBlockJob : IJob
    {
        // Obstacles are packed as (x, z, radius) on the navigation plane.
        [ReadOnly] public NativeArray<float3> Obstacles;
        [WriteOnly] public NativeArray<byte> Blocked;
        public float2 GridOrigin;

        public float CellSize;
        public float Inflation;

        public int Width;

        public void Execute()
        {
            for (int i = 0; i < Blocked.Length; i++) Blocked[i] = 0;
            int height = Blocked.Length / Width;
            for (int i = 0; i < Obstacles.Length; i++)
            {
                float3 obstacle = Obstacles[i];
                float safeRadius = obstacle.z + Inflation;
                float radiusSquared = safeRadius * safeRadius;
                int firstX = FindBound(GridOrigin.x, Width, obstacle.x, radiusSquared, false);
                int endX = FindBound(GridOrigin.x, Width, obstacle.x, radiusSquared, true);
                int firstY = FindBound(GridOrigin.y, height, obstacle.y, radiusSquared, false);
                int endY = FindBound(GridOrigin.y, height, obstacle.y, radiusSquared, true);
                for (int y = firstY; y < endY; y++)
                {
                    for (int x = firstX; x < endX; x++)
                    {
                        float2 node = GridOrigin + new float2(x, y) * CellSize;
                        if (math.distancesq(node, obstacle.xy) < radiusSquared)
                            Blocked[y * Width + x] = 1;
                    }
                }
            }
        }

        // Binary search the actual rounded node coordinates, avoiding inward-rounded
        // world-to-cell bounds. Each excluded axis alone fails the original circle test.
        private int FindBound(float origin, int count, float center, float radiusSquared, bool upper)
        {
            int first = 0;
            int end = count;
            while (first < end)
            {
                int middle = first + (end - first) / 2;
                float node = origin + middle * CellSize;
                float delta = node - center;
                bool outside = delta * delta >= radiusSquared;
                bool advance = upper ? !(node > center && outside) : node < center && outside;
                if (advance) first = middle + 1;
                else end = middle;
            }
            return first;
        }
    }
}
