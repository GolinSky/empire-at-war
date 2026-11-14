using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Movement;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace EmpireAtWar.Services.ShipNavigation
{
    internal static class ShipTangentRouteBuilder
    {
        private const float COINCIDENT_TOLERANCE = 0.001f;
        // An endpoint on an obstacle's clearance edge joins the mesh through a link this short;
        // a turn rounded there, or a start circle this small, is an in-place turn with a heading kink.
        private const float MIN_TURN_RADIUS = 0.05f;

        public static ShipRoute Build(IShipPathGraph graph, IReadOnlyList<Vector3> waypoints,
            Vector3 forward, float turnRadius, bool allowEscape)
        {
            // Dropping an endpoint link can leave a disk repair with no consistent side;
            // the uncollapsed corners still route there, with an in-place turn at the link.
            return Build(graph, waypoints, forward, turnRadius, allowEscape, true) ??
                Build(graph, waypoints, forward, turnRadius, allowEscape, false);
        }

        private static ShipRoute Build(IShipPathGraph graph, IReadOnlyList<Vector3> waypoints,
            Vector3 forward, float turnRadius, bool allowEscape, bool collapseLinks)
        {
            using var circleStorage = new NativeList<ShipRouteCircle>(waypoints.Count + 4, Allocator.TempJob);
            var circles = circleStorage;
            using var pieces = new NativeList<ShipRoutePiece>(waypoints.Count * 2 + 4, Allocator.TempJob);
            float height = waypoints[0].y;
            if (waypoints.Count == 2 && (waypoints[1] - waypoints[0]).sqrMagnitude <= ShipRouteGeometry.EPSILON * ShipRouteGeometry.EPSILON)
                return new ShipRoute(new[] { ShipRoutePiece.Line(waypoints[0], waypoints[1]) });
            int first = allowEscape && waypoints.Count > 2 ? 1 : 0;
            float2 start = ShipRouteGeometry.Planar(waypoints[first]);
            if (first > 0) pieces.Add(ShipRouteGeometry.Line(ShipRouteGeometry.Planar(waypoints[0]), start, height));
            circles.Add(new ShipRouteCircle(start, 0f, 1));
            float2 goal = ShipRouteGeometry.Planar(waypoints[waypoints.Count - 1]);
            bool tagged = graph.RouteCorners.Count == waypoints.Count && waypoints.Count > 2;
            bool collapsed = false;
            for (int i = first + 1; i < waypoints.Count - 1; i++)
            {
                float2 position = ShipRouteGeometry.Planar(waypoints[i]);
                int diskIndex = tagged ? graph.RouteCorners[i].DiskIndex : -1;
                float2 incoming = math.normalizesafe(position - ShipRouteGeometry.Planar(waypoints[i - 1]));
                float2 outgoing = math.normalizesafe(ShipRouteGeometry.Planar(waypoints[i + 1]) - position);
                float turn = math.atan2(-ShipRouteGeometry.Cross(incoming, outgoing), math.dot(incoming, outgoing));
                int sign = turn < 0f ? -1 : 1;
                if (diskIndex >= 0)
                {
                    float3 disk = graph.Disks[diskIndex];
                    int taggedSign = graph.RouteCorners[i].TurnSign;
                    if (taggedSign != 0) sign = -taggedSign;
                    ShipRouteCircle previous = circles[circles.Length - 1];
                    if (previous.DiskIndex == diskIndex && previous.Sign == sign)
                    {
                        previous.SweepHint += math.abs(turn);
                        circles[circles.Length - 1] = previous;
                    }
                    else
                    {
                        var circle = new ShipRouteCircle(disk.xy, disk.z, sign, diskIndex) { SweepHint = math.abs(turn) };
                        circles.Add(circle);
                    }
                }
                else
                {
                    float half = math.abs(turn) * 0.5f;
                    if (half < ShipRouteGeometry.EPSILON) continue;
                    if (collapseLinks && (i == first + 1 && math.distance(position, start) <= MIN_TURN_RADIUS ||
                        i == waypoints.Count - 2 && math.distance(position, goal) <= MIN_TURN_RADIUS))
                    {
                        collapsed = true;
                        continue;
                    }
                    float tangent = math.tan(half);
                    float shorter = math.min(math.distance(position, ShipRouteGeometry.Planar(waypoints[i - 1])),
                        math.distance(position, ShipRouteGeometry.Planar(waypoints[i + 1])));
                    float radius = math.min(turnRadius, 0.5f * shorter / tangent);
                    if (tagged && graph.RouteCorners[i].PortalWidth > 0f)
                        radius = math.min(radius, 0.5f * graph.RouteCorners[i].PortalWidth /
                            math.max(1f / math.cos(half) - 1f, ShipRouteGeometry.EPSILON));
                    float2 offset = -incoming * tangent + ShipRouteGeometry.Right(incoming) * sign;
                    radius = LimitRadius(graph, position, offset, radius);
                    float2 center = position + offset * radius;
                    // Equal turns at both ends of a short leg get fillets capped at half that leg,
                    // which lie on one circle; coincident circles have no tangent, so keep it once.
                    ShipRouteCircle previous = circles[circles.Length - 1];
                    if (previous.DiskIndex < 0 && previous.Sign == sign && previous.Radius > 0f &&
                        math.distance(previous.Center, center) + math.abs(previous.Radius - radius) <= COINCIDENT_TOLERANCE)
                    {
                        previous.SweepHint += math.abs(turn);
                        circles[circles.Length - 1] = previous;
                        continue;
                    }
                    circles.Add(new ShipRouteCircle(center, radius, sign) { SweepHint = math.abs(turn) });
                }
            }
            circles.Add(new ShipRouteCircle(goal, 0f, 1));
            bool directEscape = allowEscape && first == 0;
            if (first == 0 && math.distancesq(start, goal) > ShipRouteGeometry.EPSILON)
            {
                // Try both turn directions; turning towards the first corner's wrap side
                // can sweep almost a full circle when that corner lies on the other side.
                float2 direction = math.normalizesafe(ShipRouteGeometry.Planar(forward), new float2(0f, 1f));
                float shortest = float.MaxValue;
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    if (!TryStartCircle(graph, start, direction, circles[1], sign, turnRadius, directEscape,
                            out ShipRouteCircle circle, out float length) || length >= shortest) continue;
                    shortest = length;
                    circles[0] = circle;
                }
            }
            using var failure = new NativeReference<int>(Allocator.TempJob);
            new ShipTangentRouteJob
            {
                Disks = graph.Disks, DiskBuckets = graph.DiskBuckets, Layout = graph.Layout,
                Circles = circles, Pieces = pieces, Failure = failure, Start = start, Height = height,
                Escape = directEscape
            }.Run();
            if (failure.Value != ShipTangentRouteJob.FAILURE_NONE)
            {
                if (collapsed) return null;
                string message = $"[ShipNavigation] Tangent route build failed (code {failure.Value}); " +
                    "following the funnel polyline with turns in place.";
                if (failure.Value == ShipTangentRouteJob.FAILURE_NO_TANGENT)
                    Debug.LogWarning(message);
                else
                    Debug.LogError(message);
                return BuildPolyline(waypoints);
            }
            if (first > 0) FilletEscapeJoin(graph, pieces, turnRadius, height);
            if (pieces.Length == 0) pieces.Add(ShipRouteGeometry.Line(start, goal, height));
            return new ShipRoute(pieces.AsArray().ToArray());
        }

        private static bool TryStartCircle(IShipPathGraph graph, float2 start, float2 direction,
            ShipRouteCircle target, int sign, float turnRadius, bool allowEscape,
            out ShipRouteCircle circle, out float length)
        {
            float2 normal = ShipRouteGeometry.Right(direction) * sign;
            float radius = LimitRadius(graph, start, normal, turnRadius, allowEscape);
            // Keep the start circle disjoint from the first corner circle so both tangents exist.
            float2 delta = target.Center - start;
            float denominator = 2f * (math.dot(normal, delta) + target.Radius);
            if (denominator > 0f)
                radius = math.min(radius, math.max(0f, (math.lengthsq(delta) - target.Radius * target.Radius) /
                    denominator - ShipRouteGeometry.EPSILON));
            circle = new ShipRouteCircle(start + normal * radius, radius, sign);
            length = 0f;
            if (radius <= MIN_TURN_RADIUS ||
                !ShipRouteGeometry.Tangent(circle, target, out float2 exit, out float2 entry)) return false;
            float sweep = ShipRouteGeometry.Sweep(start - circle.Center, exit - circle.Center, sign);
            if (sweep < -ShipRouteGeometry.EPSILON) sweep += 2f * math.PI;
            length = math.max(0f, sweep) * radius + math.distance(exit, entry);
            return true;
        }

        // The funnel polyline lies in walkable triangles; the follower turns in place at each corner.
        private static ShipRoute BuildPolyline(IReadOnlyList<Vector3> waypoints)
        {
            var pieces = new List<ShipRoutePiece>(waypoints.Count);
            for (int i = 1; i < waypoints.Count; i++)
            {
                Vector3 delta = waypoints[i] - waypoints[i - 1];
                if (delta.x * delta.x + delta.z * delta.z <= ShipRouteGeometry.EPSILON * ShipRouteGeometry.EPSILON)
                    continue;
                pieces.Add(ShipRoutePiece.Line(waypoints[i - 1], waypoints[i]));
            }
            if (pieces.Count == 0) pieces.Add(ShipRoutePiece.Line(waypoints[0], waypoints[waypoints.Count - 1]));
            return new ShipRoute(pieces);
        }

        private static void FilletEscapeJoin(IShipPathGraph graph, NativeList<ShipRoutePiece> pieces, float turnRadius, float height)
        {
            if (pieces.Length < 2 || pieces[1].Curvature != 0f) return;
            ShipRoutePiece escape = pieces[0];
            ShipRoutePiece next = pieces[1];
            float2 incoming = new float2(math.sin(escape.Heading), math.cos(escape.Heading));
            float2 outgoing = new float2(math.sin(next.Heading), math.cos(next.Heading));
            float turn = math.atan2(-ShipRouteGeometry.Cross(incoming, outgoing), math.dot(incoming, outgoing));
            if (math.abs(turn) < ShipRouteGeometry.EPSILON) return;
            int sign = turn < 0f ? -1 : 1;
            float tangent = math.tan(math.abs(turn) * 0.5f);
            float2 anchor = ShipRouteGeometry.Planar(next.Start);
            float2 offset = -incoming * tangent + ShipRouteGeometry.Right(incoming) * sign;
            float radius = LimitRadius(graph, anchor, offset,
                math.min(turnRadius, 0.5f * math.min(escape.Length, next.Length) / tangent));
            if (radius <= ShipRouteGeometry.EPSILON) return;
            float trim = radius * tangent;
            float2 entry = anchor - incoming * trim;
            var circle = new ShipRouteCircle(anchor + offset * radius, radius, sign);
            ShipRoutePiece arc = ShipRouteGeometry.Arc(circle, entry, math.abs(turn), height);
            if (!ShipRouteGeometry.InsideBounds(arc, graph.Layout)) return;
            pieces[0] = ShipRouteGeometry.Line(ShipRouteGeometry.Planar(escape.Start), entry, height);
            pieces[1] = ShipRouteGeometry.Line(anchor + outgoing * trim, ShipRouteGeometry.Point(next, next.Length), height);
            pieces.Add(default);
            for (int i = pieces.Length - 1; i > 1; i--) pieces[i] = pieces[i - 1];
            pieces[1] = arc;
        }

        private static float LimitRadius(IShipPathGraph graph, float2 anchor, float2 offset, float radius,
            bool allowEscape = false)
        {
            // The entire candidate circle is outside neighbouring disks. Solve the first
            // contact of |disk - (anchor + offset*r)| = diskRadius + r analytically.
            float2 extentMin = math.min(anchor, anchor + offset * radius - radius);
            float2 extentMax = math.max(anchor, anchor + offset * radius + radius);
            int2 first = graph.Layout.Cell(extentMin);
            int2 last = graph.Layout.Cell(extentMax);
            for (int y = first.y; y <= last.y; y++)
            for (int x = first.x; x <= last.x; x++)
            foreach (int index in graph.DiskBuckets.GetValuesForKey(NavMeshBuckets.Key(x, y)))
            {
                float3 disk = graph.Disks[index];
                float2 delta = disk.xy - anchor;
                float c = math.lengthsq(delta) - disk.z * disk.z;
                float b = math.dot(delta, offset) + disk.z;
                float a = math.lengthsq(offset) - 1f;
                if (c < 0f && allowEscape) continue; // CheckStart validates the actual escape arc.
                if (c <= 0f) return 0f;
                if (b <= 0f) continue;
                float discriminant = b * b - a * c;
                if (discriminant < 0f) continue;
                radius = math.min(radius, c / (b + math.sqrt(discriminant)));
            }
            // Full-circle bounds are conservative for the start, and exact arc bounds
            // are checked in the job. Keep the candidate radius here for that check.
            return math.max(0f, radius);
        }
    }
}
