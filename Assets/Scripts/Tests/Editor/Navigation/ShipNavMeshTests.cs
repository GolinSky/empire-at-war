using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Entities.Map.Hazards;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Services.ShipNavigation;
using EmpireAtWar.Utils;
using NUnit.Framework;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using andywiecko.BurstTriangulator;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Navigation")]
    public sealed class ShipNavMeshTests
    {
        private const float HEIGHT = -241f;
        private const float CLEARANCE = 3f;
        private const float GEOMETRY_TOLERANCE = 0.001f;
        private static readonly IHazardSource[] _noHazards = Array.Empty<IHazardSource>();

[Test]
        public void TriangulationPreservesInputPointOrderForDiskTags()
        {
            using var points = new NativeArray<float2>(new[]
            {
                new float2(-20f, -20f), new float2(20f, 20f),
                new float2(-20f, 20f), new float2(20f, -20f),
                new float2(5f, 0f), new float2(0f, 5f),
                new float2(-5f, 0f), new float2(0f, -5f)
            }, Allocator.TempJob);
            using var positions = new NativeList<float2>(Allocator.TempJob);
            using var triangles = new NativeList<int>(Allocator.TempJob);
            using var halfedges = new NativeList<int>(Allocator.TempJob);
            using var status = new NativeReference<Status>(Allocator.TempJob);
            new NavMeshTriangulateJob
            {
                Points = points, Positions = positions, Triangles = triangles,
                Halfedges = halfedges, Status = status
            }.Run();

            Assert.That(status.Value, Is.EqualTo(Status.OK));
            Assert.That(positions.Length, Is.EqualTo(points.Length));
            for (int i = 0; i < points.Length; i++)
                Assert.That(positions[i], Is.EqualTo(points[i]), $"Input point {i} reordered.");
        }

        [TestCase(-15f)]
        [TestCase(15f)]
        public void FunnelCornersRetainDiskIdentityAndGeometricTurnSign(float offset)
        {
            var obstacle = new RadarContact(Vector3.zero, 20f, false);
            var origin = new Vector3(-80f, HEIGHT, offset);
            var destination = new Vector3(80f, HEIGHT, offset);
            using var cache = new ShipNavigationCache();
            IShipPathGraph graph = cache.GetGraph(new[] { obstacle }, Bounds(100f, 100f),
                origin, CLEARANCE, _noHazards);
            var path = new List<Vector3>();
            Assert.That(graph.TryFindPath(destination, HEIGHT, path), Is.True);
            Assert.That(graph.RouteCorners.Count, Is.EqualTo(path.Count));
            Assert.That(graph.Disks[0].z, Is.EqualTo(obstacle.Radius + CLEARANCE));
            int tagged = 0;
            for (int i = 1; i < path.Count - 1; i++)
            {
                NavMeshCorner corner = graph.RouteCorners[i];
                Assert.That(corner.Position, Is.EqualTo(path[i]));
                Assert.That(corner.DiskIndex, Is.EqualTo(0));
                Assert.That(corner.PortalWidth, Is.GreaterThan(0f));
                Vector3 incoming = path[i] - path[i - 1];
                Vector3 outgoing = path[i + 1] - path[i];
                float cross = incoming.x * outgoing.z - incoming.z * outgoing.x;
                Assert.That(corner.TurnSign, Is.EqualTo((int)Mathf.Sign(cross)));
                tagged++;
            }
            Assert.That(tagged, Is.GreaterThan(0));
            Assert.That(graph.RouteCorners[0].DiskIndex, Is.EqualTo(-1));
            Assert.That(graph.RouteCorners[path.Count - 1].DiskIndex, Is.EqualTo(-1));
        }

        [TestCase(73191)]
        [TestCase(104729)]
        [TestCase(20261106)]
        public void GridReachablePairsStayConnectedWithComparableLength(int seed)
        {
            var random = new System.Random(seed);
            Vector2Range bounds = Bounds(100f, 100f);
            var obstacles = new RadarContact[7];
            obstacles[0] = new RadarContact(Vector3.zero, 14f, false);
            obstacles[1] = obstacles[0]; // Coincident disks contribute the same ring vertices.
            for (int i = 2; i < obstacles.Length; i++)
                obstacles[i] = new RadarContact(new Vector3(random.Next(-50, 51), 0f, random.Next(-50, 51)),
                    random.Next(6, 15), false);

            var origin = new Vector3(-84f, HEIGHT, 0f);
            using var cache = new ShipNavigationCache();
            using var grid = new ShipPathGrid(obstacles, bounds, origin, CLEARANCE, _noHazards);
            IShipPathGraph mesh = cache.GetGraph(obstacles, bounds, origin, CLEARANCE, _noHazards);
            Assert.That(mesh, Is.TypeOf<ShipNavMesh>());
            var gridPath = new List<Vector3>();
            var meshPath = new List<Vector3>();
            foreach (float z in new[] { 0f, -32f, 32f })
            {
                var goal = new Vector3(84f, HEIGHT, z);
                Assert.That(grid.TryFindPath(goal, HEIGHT, gridPath), Is.True, $"Oracle seed {seed}, goal {goal}");
                Assert.That(mesh.TryFindPath(goal, HEIGHT, meshPath), Is.True, $"Seed {seed}, goal {goal}");
                Assert.That(mesh.IsKnownUnreachable(goal), Is.False);
                Assert.That(meshPath.Count, Is.GreaterThanOrEqualTo(2));
                Assert.That(meshPath[0], Is.EqualTo(origin));
                Assert.That(meshPath[meshPath.Count - 1], Is.EqualTo(goal));
                AssertClear(meshPath, obstacles, CLEARANCE);
                // Both axes use 4-unit grid cells. Allow two cells for a different corridor choice.
                Assert.That(Length(meshPath), Is.LessThanOrEqualTo(Length(gridPath) + 8f),
                    $"Seed {seed}, goal {goal}");
            }
        }

        [Test]
        public void QuasarFitsGapAndTitanFindsNearestReachableOnItsOwnSide()
        {
            Vector2Range bounds = Bounds(600f, 120f);
            RadarContact[] obstacles =
            {
                new RadarContact(new Vector3(0f, HEIGHT, 83.5f), 59.25f, false),
                new RadarContact(new Vector3(0f, HEIGHT, -83.5f), 59.25f, false)
            };
            var left = new Vector3(-500f, HEIGHT, 0f);
            var right = new Vector3(500f, HEIGHT, 0f);
            const float QUASAR_RADIUS = 22.751286f;
            const float TITAN_RADIUS = 265.9348f;
            using var cache = new ShipNavigationCache();
            IShipPathGraph quasar = cache.GetGraph(obstacles, bounds, left, QUASAR_RADIUS, _noHazards);
            var path = new List<Vector3>();
            Assert.That(quasar.TryFindPath(right, HEIGHT, path), Is.True);
            AssertClear(path, obstacles, QUASAR_RADIUS);
            Assert.That(path[0], Is.EqualTo(left));
            Assert.That(path[path.Count - 1], Is.EqualTo(right));

            IShipPathGraph titan = cache.GetGraph(obstacles, bounds, left, TITAN_RADIUS, _noHazards);
            Assert.That(titan.TryFindPath(right, HEIGHT, path), Is.False);
            Assert.That(path, Is.Empty);
            Assert.That(titan.IsKnownUnreachable(right), Is.True);
            Assert.That(titan.TryGetNearestReachable(right, HEIGHT, out Vector3 nearest), Is.True);
            Assert.That(nearest.x, Is.InRange(left.x + 1f, -1f));
            Assert.That(titan.TryFindPath(nearest, HEIGHT, path), Is.True);
            AssertClear(path, obstacles, TITAN_RADIUS);
            Assert.That(path[0], Is.EqualTo(left));
            Assert.That(path[path.Count - 1], Is.EqualTo(nearest));

            titan.SetOrigin(right);
            Assert.That(titan.TryFindPath(left, HEIGHT, path), Is.False);
            Assert.That(titan.IsKnownUnreachable(left), Is.True);
            Assert.That(titan.TryGetNearestReachable(left, HEIGHT, out nearest), Is.True);
            Assert.That(nearest.x, Is.InRange(1f, right.x - 1f));
        }

        [Test]
        public void GoalInsideInflatedDiskIsUnreachableWhileGoalJustOutsideIsReachable()
        {
            Vector2Range bounds = Bounds(100f, 100f);
            var obstacles = new[] { new RadarContact(Vector3.zero, 12f, false) };
            var origin = new Vector3(-75f, HEIGHT, 0f);
            using var cache = new ShipNavigationCache();
            IShipPathGraph graph = cache.GetGraph(obstacles, bounds, origin, CLEARANCE, _noHazards);
            var path = new List<Vector3>();
            var inside = new Vector3(12f + CLEARANCE - 0.5f, HEIGHT, 0f);
            var outside = new Vector3(12f + CLEARANCE + 2f, HEIGHT, 0f);
            Assert.That(graph.TryFindPath(inside, HEIGHT, path), Is.False);
            Assert.That(graph.IsKnownUnreachable(inside), Is.True);
            Assert.That(graph.TryFindPath(outside, HEIGHT, path), Is.True);
            Assert.That(graph.IsKnownUnreachable(outside), Is.False);
            AssertClear(path, obstacles, CLEARANCE);
        }

        [Test]
        public void EmbeddedOriginEscapesOutwardBeforeRoutingAroundObstacle()
        {
            Vector2Range bounds = Bounds(100f, 100f);
            var obstacles = new[] { new RadarContact(Vector3.zero, 12f, false) };
            var origin = new Vector3(8f, HEIGHT, 0f);
            var goal = new Vector3(-75f, HEIGHT, 0f);
            using var cache = new ShipNavigationCache();
            IShipPathGraph graph = cache.GetGraph(obstacles, bounds, origin, CLEARANCE, _noHazards);
            var path = new List<Vector3>();
            Assert.That(graph.TryFindPath(goal, HEIGHT, path), Is.True);
            Assert.That(path[0], Is.EqualTo(origin));
            Assert.That(path[path.Count - 1], Is.EqualTo(goal));
            AssertClear(path, obstacles, CLEARANCE, allowEscape: true);
        }

        [Test]
        public void EmbeddedBetweenUnequalDisksEscapesWithoutApproachingEitherCenter()
        {
            var obstacles = new[]
            {
                new RadarContact(new Vector3(-5f, HEIGHT, 0f), 7f, false),
                new RadarContact(new Vector3(5f, HEIGHT, 0f), 9f, false)
            };
            var origin = new Vector3(0f, HEIGHT, 0f);
            var goal = new Vector3(75f, HEIGHT, 0f);
            using var cache = new ShipNavigationCache();
            IShipPathGraph graph = cache.GetGraph(obstacles, Bounds(100f, 100f), origin, CLEARANCE, _noHazards);
            var path = new List<Vector3>();

            Assert.That(graph.TryFindPath(goal, HEIGHT, path), Is.True);
            Assert.That(path.Count, Is.GreaterThan(2));
            Assert.That(path[0], Is.EqualTo(origin));
            Assert.That(path[path.Count - 1], Is.EqualTo(goal));
            AssertClear(path, obstacles, CLEARANCE, allowEscape: true);
        }

        [Test]
        public void DiskCoveringEntireMapHasNoRouteOrNearestPoint()
        {
            var bounds = Bounds(100f, 100f);
            var obstacle = new RadarContact(Vector3.zero, 200f, false);
            var goal = new Vector3(80f, HEIGHT, 0f);
            using var cache = new ShipNavigationCache();
            IShipPathGraph graph = cache.GetGraph(new[] { obstacle }, bounds,
                new Vector3(-80f, HEIGHT, 0f), CLEARANCE, _noHazards);
            Assert.That(graph.TryFindPath(goal, HEIGHT, new List<Vector3>()), Is.False);
            Assert.That(graph.IsKnownUnreachable(goal), Is.True);
            Assert.That(graph.TryGetNearestReachable(goal, HEIGHT, out _), Is.False);
        }

        [Test]
        public void BatchReachabilityMatchesScalarAtDiskEdgesAndMapBoundaries()
        {
            var obstacles = new[]
            {
                new RadarContact(Vector3.zero, 20f, false),
                new RadarContact(new Vector3(32f, 0f, 8f), 13f, false)
            };
            using var cache = new ShipNavigationCache();
            IShipPathGraph graph = cache.GetGraph(obstacles, Bounds(100f, 100f),
                new Vector3(-80f, HEIGHT, 0f), CLEARANCE, _noHazards);
            var points = new List<Vector3>();
            for (int z = -105; z <= 105; z += 5)
                for (int x = -105; x <= 105; x += 5)
                    points.Add(new Vector3(x, HEIGHT, z));
            points.Add(new Vector3(23f, HEIGHT, 0f));
            points.Add(new Vector3(22.999f, HEIGHT, 0f));
            points.Add(new Vector3(23.001f, HEIGHT, 0f));
            Vector3[] positions = points.ToArray();
            var actual = new bool[positions.Length];

            graph.FillReachability(positions, actual);

            for (int i = 0; i < positions.Length; i++)
                Assert.That(actual[i], Is.EqualTo(graph.IsReachable(positions[i])), $"Point {positions[i]}");
        }

        private static Vector2Range Bounds(float x, float z)
        {
            var bounds = new Vector2Range();
            Type type = typeof(Vector2Range).BaseType;
            type.GetField("<Min>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(bounds, new Vector2(-x, -z));
            type.GetField("<Max>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(bounds, new Vector2(x, z));
            return bounds;
        }

        private static void AssertClear(List<Vector3> path, IReadOnlyList<RadarContact> obstacles,
            float clearance, bool allowEscape = false)
        {
            Assert.That(path.Count, Is.GreaterThanOrEqualTo(2));
            foreach (Vector3 point in path)
                Assert.That(point.y, Is.EqualTo(HEIGHT));
            for (int i = 1; i < path.Count; i++)
            {
                var start = new Vector2(path[i - 1].x, path[i - 1].z);
                var end = new Vector2(path[i].x, path[i].z);
                foreach (RadarContact obstacle in obstacles)
                {
                    var center = new Vector2(obstacle.Position.x, obstacle.Position.z);
                    float radius = obstacle.Radius + clearance;
                    Vector2 offset = start - center;
                    if (allowEscape && offset.sqrMagnitude < radius * radius)
                    {
                        // Squared distance increases throughout a straight leg iff its initial slope is nonnegative.
                        Assert.That(Vector2.Dot(offset, end - start), Is.GreaterThanOrEqualTo(0f),
                            $"Escape segment {start} → {end} moves toward disk at {center}");
                        continue;
                    }
                    float distance = Mathf.Sqrt(PlanarGeometry.DistanceToSegmentSquared(center, start, end));
                    Assert.That(distance, Is.GreaterThanOrEqualTo(radius - GEOMETRY_TOLERANCE),
                        $"Segment {start} → {end}, disk at {center}");
                }
            }
        }

        private static float Length(List<Vector3> path)
        {
            float length = 0f;
            for (int i = 1; i < path.Count; i++)
                length += Vector3.Distance(path[i - 1], path[i]);
            return length;
        }
    }
}
