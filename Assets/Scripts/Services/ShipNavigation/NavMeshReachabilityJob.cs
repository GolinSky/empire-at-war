using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace EmpireAtWar.Services.ShipNavigation
{
    [BurstCompile]
    internal struct NavMeshReachabilityJob : IJobParallelFor
    {
        public NavMeshBuckets Layout;
        public int OriginRegion;
        public float Clearance;
        [ReadOnly] public NativeArray<Vector3> Goals;
        [ReadOnly] public NativeArray<float3> Obstacles;
        [ReadOnly] public NativeParallelMultiHashMap<int, int> Buckets;
        [ReadOnly] public NativeArray<int> Regions;
        [ReadOnly] public NativeArray<float2> Positions;
        [ReadOnly] public NativeArray<int> Triangles;
        [WriteOnly] public NativeArray<bool> Reachable;

        public void Execute(int index)
        {
            Reachable[index] = false;
            if (OriginRegion == 0) return;
            float2 goal = new float2(Goals[index].x, Goals[index].z);
            for (int i = 0; i < Obstacles.Length; i++)
            {
                float3 disk = Obstacles[i];
                double radius = disk.z + Clearance;
                if (math.lengthsq((double2)goal - disk.xy) < radius * radius) return;
            }
            var locator = new NavMeshLocator
            {
                Layout = Layout, Point = goal, PreferredRegion = OriginRegion,
                Buckets = Buckets, Regions = Regions, Positions = Positions, Triangles = Triangles
            };
            NavMeshLocator.Result location = locator.Locate();
            Reachable[index] = location.Triangle >= 0 && Regions[location.Triangle] == OriginRegion &&
                NavMeshEscapeJob.CanConnect(location.Point, goal, Obstacles, Clearance, false);
        }
    }
}
