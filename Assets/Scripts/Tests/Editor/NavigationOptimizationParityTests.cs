using EmpireAtWar.Services.ShipNavigation;
using System.Reflection;
using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Navigation")]
    public sealed class NavigationOptimizationParityTests
    {
        [Test]
        public void ObstacleRaster_MatchesFrozenCellScan()
        {
            Random random = new Random(73191u);
            for (int sample = 0; sample < 240; sample++)
            {
                int width = random.NextInt(1, 45);
                int height = random.NextInt(1, 45);
                float size = sample % 3 == 0 ? 0.03125f : random.NextFloat(0.1f, 16f);
                float2 origin = sample % 5 == 0 ? new float2(100000f, -100000f) : random.NextFloat2(-1000f, 1000f);
                using var obstacles = new NativeArray<float3>(sample % 25, Allocator.TempJob);
                var mutableObstacles = obstacles;
                using var expected = new NativeArray<byte>(width * height, Allocator.TempJob);
                using var actual = new NativeArray<byte>(width * height, Allocator.TempJob);
                var mutableActual = actual;
                float inflation = random.NextFloat(0f, size);
                for (int i = 0; i < obstacles.Length; i++)
                {
                    float2 center = origin + random.NextFloat2(new float2(-10f), new float2(width + 10f, height + 10f)) * size;
                    mutableObstacles[i] = new float3(center, random.NextFloat(0f, 8f) * size);
                }
                new NavigationGridBlockBaselineJob
                {
                    Obstacles = obstacles, Blocked = expected, Width = width,
                    CellSize = size, GridOrigin = origin, Inflation = inflation
                }.Schedule(expected.Length, 32).Complete();
                for (int i = 0; i < actual.Length; i++) mutableActual[i] = 1;
                new NavigationGridBlockJob
                {
                    Obstacles = obstacles, Blocked = actual, Width = width,
                    CellSize = size, GridOrigin = origin, Inflation = inflation
                }.Schedule().Complete();
                CollectionAssert.AreEqual(expected.ToArray(), actual.ToArray(), $"raster {sample}");
            }
        }

        [TestCase(0f)]
        [TestCase(0.00000011920928955078125f)]
        [TestCase(-0.00000011920928955078125f)]
        public void ObstacleRaster_PreservesStrictCircleBoundary(float offset)
        {
            using var obstacles = new NativeArray<float3>(1, Allocator.TempJob);
            var mutableObstacles = obstacles;
            using var expected = new NativeArray<byte>(49, Allocator.TempJob);
            using var actual = new NativeArray<byte>(49, Allocator.TempJob);
            mutableObstacles[0] = new float3(0f, 0f, 1f + offset);
            new NavigationGridBlockBaselineJob
            {
                Obstacles = obstacles, Blocked = expected, Width = 7, CellSize = 0.5f, GridOrigin = new float2(-1.5f)
            }.Schedule(49, 8).Complete();
            new NavigationGridBlockJob
            {
                Obstacles = obstacles, Blocked = actual, Width = 7, CellSize = 0.5f, GridOrigin = new float2(-1.5f)
            }.Schedule().Complete();
            CollectionAssert.AreEqual(expected.ToArray(), actual.ToArray());
        }

        [Test]
        public void ReusedRouteBuffers_MatchFrozenSearchIncludingRollover()
        {
            Random random = new Random(57273u);
            NavigationGridCells cells = new NavigationGridCells(new float2(-40f), 2.5f, 33, 33);
            using var obstacles = new NativeArray<float3>(16, Allocator.TempJob);
            var mutableObstacles = obstacles;
            using var blocked = new NativeArray<byte>(cells.Count, Allocator.TempJob);
            using var regions = new NativeArray<int>(cells.Count, Allocator.TempJob);
            using var queue = new NativeArray<int>(cells.Count, Allocator.TempJob);
            using var buffers = new NavigationRouteParityBuffers(cells, blocked, regions, obstacles, 0.7f);
            for (int scenario = 0; scenario < 12; scenario++)
            {
                for (int i = 0; i < obstacles.Length; i++)
                    mutableObstacles[i] = new float3(random.NextFloat2(-45f, 45f), random.NextFloat(0.1f, 9f));
                new NavigationGridBlockBaselineJob
                {
                    Obstacles = obstacles, Blocked = blocked, Width = cells.Width,
                    CellSize = cells.CellSize, GridOrigin = cells.Origin, Inflation = 0.7f
                }.Schedule(cells.Count, 32).Complete();
                new NavigationRegionJob { Blocked = blocked, Cells = cells, Regions = regions, Queue = queue }.Schedule().Complete();
                for (int search = 0; search < 30; search++)
                {
                    float2 start = random.NextFloat2(-50f, 50f);
                    float2 goal = search % 7 == 0 ? start : random.NextFloat2(-50f, 50f);
                    uint generation = search == 28 ? uint.MaxValue : search == 29 ? 1u : (uint)(scenario * 30 + search + 1);
                    AssertRoute(buffers, start, goal, generation);
                }
            }
        }

        [Test]
        public void Route_DisconnectedAndFullyBlockedMapsMatchFrozenSearch()
        {
            NavigationGridCells cells = new NavigationGridCells(float2.zero, 1f, 9, 9);
            using var obstacles = new NativeArray<float3>(0, Allocator.TempJob);
            using var blocked = new NativeArray<byte>(cells.Count, Allocator.TempJob);
            var mutableBlocked = blocked;
            using var regions = new NativeArray<int>(cells.Count, Allocator.TempJob);
            using var queue = new NativeArray<int>(cells.Count, Allocator.TempJob);
            using var buffers = new NavigationRouteParityBuffers(cells, blocked, regions, obstacles, 0f);
            for (int y = 0; y < cells.Height; y++) mutableBlocked[y * cells.Width + 4] = 1;
            new NavigationRegionJob { Blocked = blocked, Cells = cells, Regions = regions, Queue = queue }.Schedule().Complete();
            AssertRoute(buffers, float2.zero, new float2(8f), 1);
            AssertRoute(buffers, float2.zero, new float2(3f), 2);
            for (int i = 0; i < blocked.Length; i++) mutableBlocked[i] = 1;
            new NavigationRegionJob { Blocked = blocked, Cells = cells, Regions = regions, Queue = queue }.Schedule().Complete();
            AssertRoute(buffers, float2.zero, new float2(8f), 1);
        }

        [Test]
        public void LineOfSight_MatchesFrozenProjectionAtBoundariesAndDegenerateSegments()
        {
            MethodInfo current = typeof(NavigationRouteJob).GetMethod("HasLineOfSight", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo baseline = typeof(NavigationRouteBaselineJob).GetMethod("HasLineOfSight", BindingFlags.Instance | BindingFlags.NonPublic);
            using var obstacles = new NativeArray<float3>(1, Allocator.TempJob);
            var mutableObstacles = obstacles;
            Random random = new Random(34917u);
            for (int sample = 0; sample < 2000; sample++)
            {
                float2 from = random.NextFloat2(-100000f, 100000f);
                float2 to = sample % 5 == 0 ? from : random.NextFloat2(-100000f, 100000f);
                float2 center = random.NextFloat2(-100000f, 100000f);
                float radius = random.NextFloat(0f, 1000f);
                if (sample % 3 == 0)
                {
                    from = new float2(-1f, 0f);
                    to = new float2(1f, 0f);
                    center = new float2(0f, 1f);
                    radius = 1f + (sample % 9 - 4) * 0.00000011920928955078125f;
                }
                mutableObstacles[0] = new float3(center, radius);
                object[] arguments = { from, to };
                bool expected = (bool)baseline.Invoke(new NavigationRouteBaselineJob { Obstacles = obstacles }, arguments);
                bool actual = (bool)current.Invoke(new NavigationRouteJob { Obstacles = obstacles }, arguments);
                Assert.That(actual, Is.EqualTo(expected), $"line of sight {sample}");
            }
        }

        private static void AssertRoute(NavigationRouteParityBuffers buffers, float2 start, float2 goal, uint generation)
        {
            buffers.Job.Start = start;
            buffers.Job.Goal = goal;
            buffers.Job.Generation = generation;
            buffers.Baseline.Start = start;
            buffers.Baseline.Goal = goal;
            buffers.Job.Schedule().Complete();
            buffers.Baseline.Schedule().Complete();
            Assert.That(buffers.Job.Status[0], Is.EqualTo(buffers.Baseline.Status[0]));
            CollectionAssert.AreEqual(buffers.Baseline.RawPath.AsArray().ToArray(), buffers.Job.RawPath.AsArray().ToArray(), "raw route");
            CollectionAssert.AreEqual(buffers.Baseline.Waypoints.AsArray().ToArray(), buffers.Job.Waypoints.AsArray().ToArray(), "simplified route");
        }
    }
}