using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Map.Generation;
using EmpireAtWar.Entities.Map.Hazards;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.ShipNavigation;
using EmpireAtWar.Utils;
using EmpireAtWar.Views.ReinforcementZones;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Random = System.Random;

namespace EmpireAtWar.Tests.Editor
{
    [TestFixture(false, Category = "Navigation", Explicit = true)]
    [TestFixture(true, Category = "NavigationExhaustive", Explicit = true)]
    public sealed class ShipArchetypeRouteSweepTests
    {
        private const float TOLERANCE = 0.01f;
        private readonly bool _exhaustive;
        private MapGenerationSettings _settings;
        private MapLayoutGenerator _generator;

        public ShipArchetypeRouteSweepTests(bool exhaustive) => _exhaustive = exhaustive;

        [SetUp]
        public void SetUp()
        {
            _settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(
                "Assets/Settings/Data/Models/Map/MapGenerationSettings.asset");
            CaptureSiteData sites = AssetDatabase.LoadAssetAtPath<CaptureSiteData>(
                "Assets/Settings/Data/Models/CaptureSites/CaptureSiteData.asset");
            CaptureSiteView mine = AssetDatabase.LoadAssetAtPath<CaptureSiteView>(
                "Assets/Prefabs/View/CaptureSites/CaptureSite.prefab");
            float radius = sites.GetSettings(CaptureSiteKind.Mining).Radius *
                Mathf.Max(Mathf.Abs(mine.transform.lossyScale.x), Mathf.Abs(mine.transform.lossyScale.z));
            _generator = new MapLayoutGenerator(_settings, new MapFeatureRadii(
                AssetDatabase.LoadAssetAtPath<ReinforcementZoneView>(
                    "Assets/Prefabs/View/ReinforcementZones/ReinforcementZone.prefab").Radius, radius,
                AssetDatabase.LoadAssetAtPath<ReinforcementZoneView>(
                    "Assets/Prefabs/View/ReinforcementZones/MainReinforcementZone.prefab").Radius));
        }

        [Test]
        public void EveryArchetype_RandomReachablePairsKeepClearanceAndContinuousMotion()
        {
            int[] seeds = _exhaustive ? Enumerable.Range(0, 20).ToArray() : new[] { 0, 7, 19 };
            foreach (PlanetMapProfileEntry profile in _settings.Profiles.Entries
                         .GroupBy(entry => entry.Archetype.Kind).Select(group => group.First()))
            foreach (int seed in seeds)
            {
                int count = Math.Min(MatchRules.GetMaxPlayers(MapSize.Large), profile.Archetype.MaximumPlayers);
                PlayerSlot[] players = Enumerable.Range(0, count).Select(index => new PlayerSlot(
                    new PlayerId(index), new TeamId(index), index % 2 == 0 ? FactionType.Republic : FactionType.Separatist,
                    index == 0 ? PlayerController.Human : PlayerController.Ai, EnemyAiDifficulty.Medium, index)).ToArray();
                MapLayout layout = _generator.Generate(MapSize.Large, players, new Random(seed), seed, profile.Planet);
                var obstacles = new List<RadarContact>();
                foreach (AsteroidField field in layout.Fields)
                foreach (FieldVolume volume in field.Volumes)
                    obstacles.Add(new RadarContact(volume.Center, volume.Radius, false));
                foreach (PlayerSlot player in players)
                    obstacles.Add(new RadarContact(layout.GetStationPosition(player.Id),
                        _settings.GetStationRadius(player.Faction), false));
                if (layout.PlanetObstacleRadius > 0f)
                    obstacles.Add(new RadarContact(layout.PlanetPosition, layout.PlanetObstacleRadius, false));
                foreach (float clearance in new[] { 4f, 22.75f, 70f })
                    Sweep(layout, obstacles, clearance, seed,
                        $"{profile.Archetype.Kind}/Large/{count}/seed={seed}/clearance={clearance}");
            }
        }

        private void Sweep(MapLayout layout, List<RadarContact> obstacles, float clearance, int seed, string context)
        {
            using var cache = new ShipNavigationCache();
            IShipPathGraph graph = cache.GetGraph(obstacles, layout.SizeRange, layout.Zones[0].Center,
                clearance, layout.Hazards.Cast<IHazardSource>().ToArray());
            var random = new Random(seed ^ 0x51A7);
            var points = new List<Vector3>();
            int requested = _exhaustive ? 24 : 6;
            int completed = 0;
            double totalMilliseconds = 0d;
            double maximumMilliseconds = 0d;
            for (int attempt = 0; attempt < requested * 100 && completed < requested; attempt++)
            {
                Vector3 start = RandomPoint(layout, random);
                Vector3 end = RandomPoint(layout, random);
                if (!graph.IsReachable(start) || !graph.IsReachable(end)) continue;
                graph.SetOrigin(start);
                if (!graph.TryFindPath(end, 0f, points)) continue;
                Vector3 forward = points[1] - points[0];
                var timer = Stopwatch.StartNew();
                ShipRoute route = BuildRoute(graph, points, forward, out bool fallback);
                timer.Stop();
                totalMilliseconds += timer.Elapsed.TotalMilliseconds;
                maximumMilliseconds = Math.Max(maximumMilliseconds, timer.Elapsed.TotalMilliseconds);
                AssertRoute(route, graph, fallback, $"{context}/sample={completed}/start={start:F3}/end={end:F3}");
                completed++;
            }
            Assert.That(completed, Is.EqualTo(requested), context + " insufficient reachable sampled pairs");
            TestContext.WriteLine($"{context}: routes={completed}; build mean={totalMilliseconds / completed:F3} ms; " +
                $"max={maximumMilliseconds:F3} ms");
        }

        private static Vector3 RandomPoint(MapLayout layout, Random random) => new Vector3(
            Mathf.Lerp(layout.SizeRange.Min.x, layout.SizeRange.Max.x, (float)random.NextDouble()), 0f,
            Mathf.Lerp(layout.SizeRange.Min.y, layout.SizeRange.Max.y, (float)random.NextDouble()));

        private static ShipRoute BuildRoute(IShipPathGraph graph, IReadOnlyList<Vector3> points, Vector3 forward,
            out bool fallback)
        {
            bool usedFallback = false;
            Application.logMessageReceived += HandleBuildLog;
            try
            {
                ShipRoute route = ShipTangentRouteBuilder.Build(graph, points, forward, 30f, false);
                fallback = usedFallback;
                return route;
            }
            finally
            {
                Application.logMessageReceived -= HandleBuildLog;
            }

            void HandleBuildLog(string message, string stackTrace, LogType type)
            {
                if (type == LogType.Warning && IsFallback(ShipTangentRouteJob.FAILURE_NO_TANGENT))
                    usedFallback = true;
                if (type == LogType.Error && (IsFallback(ShipTangentRouteJob.FAILURE_ARC_REPAIR) ||
                    IsFallback(ShipTangentRouteJob.FAILURE_LINE_REPAIR)))
                {
                    usedFallback = true;
                    LogAssert.Expect(type, message);
                }

                bool IsFallback(int code) => message == $"[ShipNavigation] Tangent route build failed (code {code}); " +
                    "following the funnel polyline with turns in place.";
            }
        }

        private static void AssertRoute(ShipRoute route, IShipPathGraph graph, bool fallback, string context)
        {
            var speedProfile = new ShipRouteSpeedProfile(route, 20f, 30f, 0f);
            int phase = 0;
            for (int i = 0; i < route.Pieces.Count; i++)
            {
                ShipRoutePiece piece = route.Pieces[i];
                if (i > 0)
                {
                    Assert.That(Vector3.Distance(route.Pieces[i - 1].End, piece.Start), Is.LessThan(TOLERANCE), context);
                    if (fallback && route.RequiresTurnAtPiece(i))
                    {
                        speedProfile.EvaluateTime(speedProfile.GetPieceStartTime(i), ref phase, out float distance, out float speed);
                        Assert.That(distance, Is.EqualTo(route.GetPieceStartDistance(i)).Within(TOLERANCE), context);
                        Assert.That(speed, Is.Zero.Within(TOLERANCE), context);
                    }
                    else if (!fallback)
                    {
                        Assert.That(Vector3.Angle(route.Pieces[i - 1].EndTangent, piece.InitialTangent), Is.LessThan(0.1f), context);
                    }
                }
                foreach (var disk in graph.Disks)
                    Assert.That(DistanceToPiece(piece, new Vector2(disk.x, disk.y)),
                        Is.GreaterThanOrEqualTo(disk.z - TOLERANCE), context);
                for (int sample = 0; sample <= 32; sample++)
                {
                    Vector3 point = piece.Evaluate(piece.Length * sample / 32f, out _);
                    Assert.That(point.x, Is.InRange(graph.Layout.Min.x - TOLERANCE, graph.Layout.Max.x + TOLERANCE), context);
                    Assert.That(point.z, Is.InRange(graph.Layout.Min.y - TOLERANCE, graph.Layout.Max.y + TOLERANCE), context);
                }
            }
            int pieceIndex = 0;
            for (int sample = 1; sample < 200; sample++)
            {
                speedProfile.EvaluateTime(speedProfile.Duration * sample / 200f, ref phase, out float distance, out float speed);
                route.EvaluateDistance(distance, ref pieceIndex, out _, out _);
                Assert.That(speed, Is.GreaterThan(0f), context);
                Assert.That(speed, Is.LessThanOrEqualTo(speedProfile.GetSpeedLimit(pieceIndex) + TOLERANCE), context);
            }
        }

        private static float DistanceToPiece(ShipRoutePiece piece, Vector2 point)
        {
            Vector2 start = new Vector2(piece.Start.x, piece.Start.z);
            Vector2 end = new Vector2(piece.End.x, piece.End.z);
            if (piece.Curvature == 0f)
                return Mathf.Sqrt(PlanarGeometry.DistanceToSegmentSquared(point, start, end));
            Vector2 center = start + new Vector2(Mathf.Cos(piece.Heading), -Mathf.Sin(piece.Heading)) / piece.Curvature;
            Vector2 from = start - center;
            Vector2 radial = point - center;
            float sweep = Mathf.Repeat(-Vector2.SignedAngle(from, radial) * Mathf.Sign(piece.Curvature), 360f) * Mathf.Deg2Rad;
            if (sweep <= piece.Length * Mathf.Abs(piece.Curvature))
                return Mathf.Abs(radial.magnitude - Mathf.Abs(1f / piece.Curvature));
            return Mathf.Min(Vector2.Distance(point, start), Vector2.Distance(point, end));
        }
    }
}
