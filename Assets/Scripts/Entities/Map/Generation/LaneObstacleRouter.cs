using System.Collections.Generic;
using EmpireAtWar.Utils;
using UnityEngine;

namespace EmpireAtWar.Entities.Map.Generation
{
    /// <summary>Routes crossing lanes around reserved shortcut blockers using a small visibility graph.</summary>
    public static class LaneObstacleRouter
    {
        private const float ROUTE_MARGIN = 2f;

        public static bool TryRoute(MapSizeSettings size, List<MapLane> lanes, IReadOnlyList<FieldVolume> blockers)
        {
            Vector3 center = PlanarGeometry.GetCenter(size.Bounds);
            float halfWidth = size.MinLaneWidth * 0.5f;
            for (int index = 0; index < lanes.Count; index++)
            {
                MapLane lane = lanes[index];
                bool crosses = false;
                foreach (FieldVolume blocker in blockers)
                    crosses |= PlanarGeometry.DistanceToPolyline(blocker.Center, lane.Points) <
                        blocker.Radius + halfWidth;
                if (!crosses) continue;

                Vector3 start = lane.Points[0];
                Vector3 end = lane.Points[lane.Points.Count - 1];
                Vector3 orientation = PlanarGeometry.Distance(start, center) > ROUTE_MARGIN
                    ? start - center : end - center;
                bool reflect = orientation.x < 0f || (Mathf.Abs(orientation.x) < ROUTE_MARGIN && orientation.z < 0f);
                if (reflect)
                {
                    start = PlanarGeometry.Reflect(start, center);
                    end = PlanarGeometry.Reflect(end, center);
                }
                List<Vector3> points = new List<Vector3> { start, end };
                foreach (FieldVolume blocker in blockers)
                {
                    float reach = blocker.Radius + halfWidth + ROUTE_MARGIN;
                    for (int x = -1; x <= 1; x += 2)
                        for (int z = -1; z <= 1; z += 2)
                        {
                            Vector3 point = blocker.Center + new Vector3(x * reach, 0f, z * reach);
                            if (point.x >= size.Bounds.Min.x && point.x <= size.Bounds.Max.x &&
                                point.z >= size.Bounds.Min.y && point.z <= size.Bounds.Max.y &&
                                IsClear(point, point, blockers, halfWidth))
                                points.Add(point);
                        }
                }
                List<Vector3> route = FindRoute(points, blockers, halfWidth);
                if (route.Count == 0) return false;
                if (reflect)
                    for (int i = 0; i < route.Count; i++)
                        route[i] = PlanarGeometry.Reflect(route[i], center);
                lanes[index] = new MapLane(route);
            }
            return true;
        }

        private static List<Vector3> FindRoute(IReadOnlyList<Vector3> points,
            IReadOnlyList<FieldVolume> blockers, float halfWidth)
        {
            float[] distance = new float[points.Count];
            int[] previous = new int[points.Count];
            bool[] visited = new bool[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                distance[i] = float.MaxValue;
                previous[i] = -1;
            }
            distance[0] = 0f;
            for (int step = 0; step < points.Count; step++)
            {
                int current = -1;
                for (int i = 0; i < points.Count; i++)
                    if (!visited[i] && distance[i] < float.MaxValue &&
                        (current < 0 || distance[i] < distance[current]))
                        current = i;
                if (current < 0) break;
                if (current == 1)
                {
                    List<Vector3> route = new List<Vector3>();
                    for (int i = 1; i >= 0; i = previous[i]) route.Add(points[i]);
                    route.Reverse();
                    return route;
                }
                visited[current] = true;
                for (int next = 0; next < points.Count; next++)
                {
                    if (visited[next] || !IsClear(points[current], points[next], blockers, halfWidth)) continue;
                    float candidate = distance[current] + PlanarGeometry.Distance(points[current], points[next]);
                    if (candidate >= distance[next]) continue;
                    distance[next] = candidate;
                    previous[next] = current;
                }
            }
            return new List<Vector3>();
        }

        private static bool IsClear(Vector3 start, Vector3 end, IReadOnlyList<FieldVolume> blockers, float halfWidth)
        {
            foreach (FieldVolume blocker in blockers)
            {
                float reach = blocker.Radius + halfWidth + ROUTE_MARGIN * 0.5f;
                if (PlanarGeometry.DistanceToSegmentSquared(new Vector2(blocker.Center.x, blocker.Center.z),
                    new Vector2(start.x, start.z), new Vector2(end.x, end.z)) < reach * reach)
                    return false;
            }
            return true;
        }
    }
}
