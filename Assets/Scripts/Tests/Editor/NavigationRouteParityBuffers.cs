using System;
using EmpireAtWar.Services.ShipNavigation;
using Unity.Collections;
using Unity.Mathematics;

namespace EmpireAtWar.Tests.Editor
{
    internal sealed class NavigationRouteParityBuffers : IDisposable
    {
        public NavigationRouteJob Job;
        public NavigationRouteBaselineJob Baseline;

        public NavigationRouteParityBuffers(NavigationGridCells cells, NativeArray<byte> blocked,
            NativeArray<int> regions, NativeArray<float3> obstacles, float inflation)
        {
            Job = new NavigationRouteJob
            {
                Cells = cells, Blocked = blocked, Regions = regions, Obstacles = obstacles,
                LineOfSightInflation = inflation,
                CostSoFar = new NativeArray<float>(cells.Count, Allocator.TempJob),
                CameFrom = new NativeArray<int>(cells.Count, Allocator.TempJob),
                Closed = new NativeArray<byte>(cells.Count, Allocator.TempJob),
                VisitedGeneration = new NativeArray<uint>(cells.Count, Allocator.TempJob),
                Open = new NativeList<NavigationRouteJob.OpenNode>(cells.Count, Allocator.TempJob),
                RawPath = new NativeList<float2>(cells.Count, Allocator.TempJob),
                Waypoints = new NativeList<float2>(cells.Count, Allocator.TempJob),
                Status = new NativeArray<int>(1, Allocator.TempJob)
            };
            Baseline = new NavigationRouteBaselineJob
            {
                Cells = cells, Blocked = blocked, Regions = regions, Obstacles = obstacles,
                LineOfSightInflation = inflation,
                CostSoFar = new NativeArray<float>(cells.Count, Allocator.TempJob),
                CameFrom = new NativeArray<int>(cells.Count, Allocator.TempJob),
                Closed = new NativeArray<byte>(cells.Count, Allocator.TempJob),
                Open = new NativeList<NavigationRouteBaselineJob.OpenNode>(cells.Count, Allocator.TempJob),
                RawPath = new NativeList<float2>(cells.Count, Allocator.TempJob),
                Waypoints = new NativeList<float2>(cells.Count, Allocator.TempJob),
                Status = new NativeArray<int>(1, Allocator.TempJob)
            };
        }

        public void Dispose()
        {
            Job.CostSoFar.Dispose();
            Job.CameFrom.Dispose();
            Job.Closed.Dispose();
            Job.VisitedGeneration.Dispose();
            Job.Open.Dispose();
            Job.RawPath.Dispose();
            Job.Waypoints.Dispose();
            Job.Status.Dispose();
            Baseline.CostSoFar.Dispose();
            Baseline.CameFrom.Dispose();
            Baseline.Closed.Dispose();
            Baseline.Open.Dispose();
            Baseline.RawPath.Dispose();
            Baseline.Waypoints.Dispose();
            Baseline.Status.Dispose();
        }
    }
}
