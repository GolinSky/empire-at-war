using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Entities.Map.Hazards;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Services.ShipNavigation;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Navigation")]
    public sealed class ShipTangentRouteBuilderTests
    {
        private const float CLEARANCE = 0.25f;
        private const float TOLERANCE = 0.002f;

        [TestCase(12f, 100f)]
        [TestCase(140f, 300f)]
        public void CentralDisk_ProducesClearContinuousArcs(float radius, float mapHalfSize)
        {
            var obstacles = new[] { new RadarContact(Vector3.zero, radius, false) };
            BuildAndAssert(obstacles, mapHalfSize, new Vector3(-mapHalfSize * 0.8f, 0f, 0f),
                new Vector3(mapHalfSize * 0.8f, 0f, 0f), Vector3.right);
        }

        [Test]
        public void OverlappingDiskWall_KeepsClearanceAndTangency()
        {
            var obstacles = new[]
            {
                new RadarContact(new Vector3(-15f, 0f, 0f), 12f, false),
                new RadarContact(Vector3.zero, 12f, false),
                new RadarContact(new Vector3(15f, 0f, 0f), 12f, false)
            };
            BuildAndAssert(obstacles, 100f, Vector3.left * 80f, Vector3.right * 80f, Vector3.right);
        }

        [Test]
        public void SmallDiskProtrudingIntoCornerArc_RemainsClear()
        {
            var obstacles = new[]
            {
                new RadarContact(Vector3.zero, 12f, false),
                new RadarContact(Vector3.forward * 11.8f, 0.5f, false),
                new RadarContact(Vector3.back * 11.8f, 0.5f, false)
            };
            BuildAndAssert(obstacles, 100f, Vector3.left * 80f, Vector3.right * 80f, Vector3.right);
        }

        [Test]
        public void RouteBesideMapEdge_StartTurnAndAllPiecesStayInsideBounds()
        {
            var obstacles = new[] { new RadarContact(new Vector3(0f, 0f, 85f), 12f, false) };
            BuildAndAssert(obstacles, 100f, new Vector3(-80f, 0f, 95f),
                new Vector3(80f, 0f, 95f), Vector3.forward);
        }

        [Test]
        public void DirectTurnaround_StaysInsideMapAndReachesDestination()
        {
            using var cache = new ShipNavigationCache();
            Vector3 start = new Vector3(0f, 0f, 95f);
            Vector3 end = new Vector3(0f, 0f, 60f);
            IShipPathGraph graph = cache.GetGraph(Array.Empty<RadarContact>(), Bounds(100f),
                start, CLEARANCE, Array.Empty<IHazardSource>());
            var points = new List<Vector3>();
            Assert.That(graph.TryFindPath(end, 0f, points), Is.True);
            ShipRoute route = ShipTangentRouteBuilder.Build(graph, points, Vector3.forward, 15f, false);
            AssertGeometry(route, Array.Empty<RadarContact>(), 100f, start, end);
        }

        [Test]
        public void StaircaseHazardBoundary_RouteContainsCurvesWithoutCornerStops()
        {
            SpaceHazardData data = ScriptableObject.CreateInstance<SpaceHazardData>();
            try
            {
                HazardShape hazard = TestHazards.CreateEllipse(data, Vector3.zero,
                    Vector3.forward, 30f, 20f);
                BuildAndAssert(Array.Empty<RadarContact>(), 100f, Vector3.left * 80f,
                    Vector3.right * 80f, Vector3.right, new IHazardSource[] { hazard });
            }
            finally
            {
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void FirstCornerAheadLeftWrappedClockwise_StartTurnTakesShortSide()
        {
            // The path passes west of the disk and then turns right around it. The start
            // circle must turn left towards that corner rather than loop around to the right.
            var obstacles = new[] { new RadarContact(new Vector3(-30f, 0f, 0f), 12f, false) };
            ShipRoute route = BuildAndAssert(obstacles, 100f, new Vector3(0f, 0f, -80f),
                new Vector3(-70f, 0f, 80f), Vector3.forward);
            foreach (ShipRoutePiece piece in route.Pieces)
                Assert.That(Mathf.Abs(piece.Length * piece.Curvature), Is.LessThan(Mathf.PI),
                    "No arc on this route needs to sweep more than half a turn.");
        }

        private static ShipRoute BuildAndAssert(RadarContact[] obstacles, float mapHalfSize,
            Vector3 start, Vector3 end, Vector3 forward, IHazardSource[] hazards = null)
        {
            using var cache = new ShipNavigationCache();
            IShipPathGraph graph = cache.GetGraph(obstacles, Bounds(mapHalfSize), start,
                CLEARANCE, hazards ?? Array.Empty<IHazardSource>());
            var points = new List<Vector3>();
            Assert.That(graph.TryFindPath(end, 0f, points), Is.True);
            ShipRoute route = ShipTangentRouteBuilder.Build(graph, points, forward, 15f, false);
            AssertGeometry(route, obstacles, mapHalfSize, start, end);
            bool curved = false;
            foreach (ShipRoutePiece piece in route.Pieces) curved |= piece.Curvature != 0f;
            Assert.That(curved, Is.True);

            var profile = new ShipRouteSpeedProfile(route, 10f, 90f, 0f);
            int phase = 0;
            for (int i = 1; i < 200; i++)
            {
                profile.EvaluateTime(profile.Duration * i / 200f, ref phase,
                    out float distance, out float speed);
                int pieceIndex = 0;
                route.EvaluateDistance(distance, ref pieceIndex, out _, out _);
                Assert.That(speed, Is.GreaterThan(0f), "Only the route endpoints may stop.");
                Assert.That(speed, Is.LessThanOrEqualTo(profile.GetSpeedLimit(pieceIndex) + TOLERANCE));
            }
            return route;
        }

        private static void AssertGeometry(ShipRoute route, IReadOnlyList<RadarContact> obstacles,
            float mapHalfSize, Vector3 start, Vector3 end)
        {
            Assert.That(Vector3.Distance(route.Pieces[0].Start, start), Is.LessThan(TOLERANCE));
            Assert.That(Vector3.Distance(route.Pieces[route.Pieces.Count - 1].End, end), Is.LessThan(TOLERANCE));
            for (int index = 0; index < route.Pieces.Count; index++)
            {
                ShipRoutePiece piece = route.Pieces[index];
                Assert.That(piece.Length, Is.GreaterThan(0f));
                if (index > 0)
                {
                    ShipRoutePiece previous = route.Pieces[index - 1];
                    Assert.That(Vector3.Distance(previous.End, piece.Start), Is.LessThan(TOLERANCE));
                    Assert.That(Vector3.Angle(previous.EndTangent, piece.InitialTangent), Is.LessThan(0.1f));
                }
                int samples = Mathf.Max(1, Mathf.CeilToInt(piece.Length / 0.25f));
                for (int sample = 0; sample <= samples; sample++)
                {
                    Vector3 position = piece.Evaluate(piece.Length * sample / samples, out _);
                    Assert.That(Mathf.Abs(position.x), Is.LessThanOrEqualTo(mapHalfSize + TOLERANCE));
                    Assert.That(Mathf.Abs(position.z), Is.LessThanOrEqualTo(mapHalfSize + TOLERANCE));
                    foreach (RadarContact obstacle in obstacles)
                    {
                        Vector2 offset = new Vector2(position.x - obstacle.Position.x,
                            position.z - obstacle.Position.z);
                        Assert.That(offset.magnitude,
                            Is.GreaterThanOrEqualTo(obstacle.Radius + CLEARANCE - TOLERANCE),
                            $"Piece {index} intersects disk at {obstacle.Position}.");
                    }
                }
            }
        }

        private static Vector2Range Bounds(float halfSize)
        {
            var bounds = new Vector2Range();
            Type type = typeof(Vector2Range).BaseType;
            type.GetField("<Min>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(bounds, new Vector2(-halfSize, -halfSize));
            type.GetField("<Max>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(bounds, new Vector2(halfSize, halfSize));
            return bounds;
        }
    }
}
