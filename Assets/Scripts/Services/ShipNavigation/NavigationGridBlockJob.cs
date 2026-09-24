using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace EmpireAtWar.Services.ShipNavigation
{
    [BurstCompile]
    internal struct NavigationGridBlockJob : IJobParallelFor
    {
        // Obstacles are packed as (x, z, radius) on the navigation plane.
        [ReadOnly] public NativeArray<float3> Obstacles;
        [WriteOnly] public NativeArray<byte> Blocked;
        public float2 GridOrigin;
        public float CellSize;
        public int Width;
        public float Inflation;

        public void Execute(int index)
        {
            float2 node = GridOrigin + new float2(index % Width, index / Width) * CellSize;
            byte blocked = 0;
            for (int i = 0; i < Obstacles.Length; i++)
            {
                float3 obstacle = Obstacles[i];
                float safeRadius = obstacle.z + Inflation;
                if (math.distancesq(node, obstacle.xy) < safeRadius * safeRadius)
                {
                    blocked = 1;
                    break;
                }
            }

            Blocked[index] = blocked;
        }
    }
}
