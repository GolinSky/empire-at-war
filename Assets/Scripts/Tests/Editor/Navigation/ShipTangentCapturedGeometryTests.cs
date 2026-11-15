using System;
using System.Collections.Generic;
using System.IO;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Entities.Map.Hazards;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Services.ShipNavigation;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Navigation")]
    public sealed class ShipTangentCapturedGeometryTests
    {
        [Test]
        public void Battle1696_TaggedFunnelBuildsWithoutRepairFailureOrPolylineFallback()
        {
            // Full builder input captured from battle frame 78862, registration 77.
            // Keep waypoint tags, endpoint link, bounds and all clearance-expanded disks exact.
            var snapshot = JsonUtility.FromJson<ShipTangentFailureSnapshot>(File.ReadAllText(
                Path.Combine(Application.dataPath, "Scripts/Tests/Editor/Navigation/ShipTangentBattle1696.json")));
            Assert.That(snapshot.BuildId, Is.EqualTo(1696));
            Assert.That(snapshot.FailureCode, Is.EqualTo(ShipTangentRouteJob.FAILURE_ARC_REPAIR));
            using var graph = new CapturedGraph(snapshot);

            ShipRoute route = ShipTangentRouteBuilder.Build(graph, snapshot.Waypoints,
                snapshot.Forward, snapshot.TurnRadius, snapshot.AllowEscape, snapshot.Agent);

            bool hasArc = false;
            foreach (ShipRoutePiece piece in route.Pieces) hasArc |= piece.Curvature != 0f;
            Assert.That(hasArc, Is.True, "The checked funnel polyline is not a tangent repair.");
            Assert.That(Vector3.Distance(route.Pieces[0].Start, snapshot.Waypoints[0]), Is.LessThan(0.002f));
            Assert.That(Vector3.Distance(route.Pieces[route.Pieces.Count - 1].End,
                snapshot.Waypoints[snapshot.Waypoints.Length - 1]), Is.LessThan(0.002f));
            for (int i = 0; i < route.Pieces.Count; i++)
            {
                ShipRoutePiece piece = route.Pieces[i];
                Assert.That(piece.Length, Is.GreaterThan(0f));
                Assert.That(ShipRouteGeometry.InsideBounds(piece, graph.Layout), Is.True, $"Piece {i}");
                foreach (float3 disk in snapshot.Disks)
                    Assert.That(ShipRouteGeometry.DistanceSquared(piece, disk.xy),
                        Is.GreaterThanOrEqualTo((disk.z - 0.002f) * (disk.z - 0.002f)),
                        $"Piece {i} analytically intersects {disk}");
                if (i > 0)
                {
                    Assert.That(Vector3.Distance(route.Pieces[i - 1].End, piece.Start), Is.LessThan(0.002f));
                    Assert.That(Vector3.Angle(route.Pieces[i - 1].EndTangent, piece.InitialTangent),
                        Is.LessThan(0.1f), $"Tangent break at piece {i}");
                }
                int samples = Mathf.Max(1, Mathf.CeilToInt(piece.Length / 0.25f));
                for (int sample = 0; sample <= samples; sample++)
                {
                    Vector3 point = piece.Evaluate(piece.Length * sample / samples, out _);
                    foreach (float3 disk in snapshot.Disks)
                        Assert.That(math.distance(new float2(point.x, point.z), disk.xy),
                            Is.GreaterThanOrEqualTo(disk.z - 0.002f), $"Piece {i} intersects {disk}");
                }
            }
        }

        private sealed class CapturedGraph : IShipPathGraph
        {
            public IReadOnlyList<NavMeshCorner> RouteCorners { get; }
            public NativeArray<float3> Disks { get; }
            public NativeParallelMultiHashMap<int, int> DiskBuckets { get; }
            public NavMeshBuckets Layout { get; }

            public CapturedGraph(ShipTangentFailureSnapshot snapshot)
            {
                var corners = new NavMeshCorner[snapshot.Corners.Length];
                for (int i = 0; i < corners.Length; i++)
                {
                    ShipTangentFailureSnapshot.Corner corner = snapshot.Corners[i];
                    corners[i] = new NavMeshCorner(corner.Position, corner.DiskIndex,
                        corner.TurnSign, corner.PortalWidth);
                }
                RouteCorners = corners;
                Disks = new NativeArray<float3>(snapshot.Disks, Allocator.TempJob);
                DiskBuckets = new NativeParallelMultiHashMap<int, int>(snapshot.Disks.Length * 16,
                    Allocator.TempJob);
                Layout = new NavMeshBuckets(snapshot.BoundsMin, snapshot.BoundsMax);
                for (int i = 0; i < Disks.Length; i++)
                    Layout.Insert(DiskBuckets, i, Disks[i].xy - Disks[i].z, Disks[i].xy + Disks[i].z);
            }

            public void Dispose()
            {
                Disks.Dispose();
                DiskBuckets.Dispose();
            }

            public bool Matches(IReadOnlyList<RadarContact> obstacles, Vector2Range mapRange,
                float clearance, IReadOnlyList<IHazardSource> hazards) => throw new NotSupportedException();
            public void SetOrigin(Vector3 origin) => throw new NotSupportedException();
            public bool IsUnpenalizedSegment(Vector3 from, Vector3 to) => throw new NotSupportedException();
            public bool TryFindPath(Vector3 destination, float height, List<Vector3> waypoints) => throw new NotSupportedException();
            public bool IsKnownUnreachable(Vector3 position) => throw new NotSupportedException();
            public bool IsReachable(Vector3 position) => throw new NotSupportedException();
            public void FillReachability(Vector3[] positions, bool[] reachable) => throw new NotSupportedException();
            public bool TryFillReachability(Vector3[] positions, bool[] reachable, bool forceRefresh) => throw new NotSupportedException();
            public bool TryGetNearestReachable(Vector3 position, float height, out Vector3 nearest) => throw new NotSupportedException();
        }
    }
}
