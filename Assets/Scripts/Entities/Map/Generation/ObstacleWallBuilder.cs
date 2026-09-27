using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace EmpireAtWar.Entities.Map.Generation
{
    /// <summary>
    /// Lines both sides of every road with asteroids and rings each walled node, so a node is
    /// entered only where a road corridor cuts through its ring.
    /// </summary>
    public sealed class ObstacleWallBuilder
    {
        // Share of the spacing below which a new asteroid would pile onto a placed one.
        private const float MIN_SPACING_SHARE = 0.7f;
        private const float EDGE_TOLERANCE = 0.5f;

        private readonly MapGenerationSettings _settings;

        public ObstacleWallBuilder(MapGenerationSettings settings)
        {
            _settings = settings;
        }

        public List<ObstacleSpot> Build(
            MapSizeSettings size,
            IReadOnlyList<MapStation> stations,
            IReadOnlyList<MapNode> nodes,
            IReadOnlyList<MapRoad> roads,
            Random random)
        {
            List<ObstacleSpot> obstacles = new List<ObstacleSpot>();
            foreach (MapNode node in nodes)
            {
                if (node.IsWalled)
                {
                    AddRing(node, size, stations, nodes, roads, random, obstacles);
                }
            }

            foreach (MapRoad road in roads)
            {
                AddRoadWalls(road, size, stations, nodes, roads, random, obstacles);
            }

            return obstacles;
        }

        private void AddRing(
            MapNode node,
            MapSizeSettings size,
            IReadOnlyList<MapStation> stations,
            IReadOnlyList<MapNode> nodes,
            IReadOnlyList<MapRoad> roads,
            Random random,
            List<ObstacleSpot> obstacles)
        {
            float innerRadius = node.Radius + _settings.ZoneClearance;
            float circumference = 2f * Mathf.PI * (innerRadius + GetHalfFootprint(size.ObstacleScale.Max));
            int count = Mathf.Max(1, Mathf.CeilToInt(circumference / size.ObstacleSpacing));
            for (int i = 0; i < count; i++)
            {
                float scale = PickScale(size, random);
                float angle = i * 2f * Mathf.PI / count;
                Vector3 position = node.Center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) *
                    (innerRadius + GetHalfFootprint(scale));
                TryAdd(position, scale, size, stations, nodes, roads, random, obstacles);
            }
        }

        private void AddRoadWalls(
            MapRoad road,
            MapSizeSettings size,
            IReadOnlyList<MapStation> stations,
            IReadOnlyList<MapNode> nodes,
            IReadOnlyList<MapRoad> roads,
            Random random,
            List<ObstacleSpot> obstacles)
        {
            for (int i = 1; i < road.Points.Count; i++)
            {
                Vector3 start = road.Points[i - 1];
                Vector3 segment = road.Points[i] - start;
                segment.y = 0f;
                float length = segment.magnitude;
                Vector3 direction = segment / length;
                Vector3 normal = new Vector3(-direction.z, 0f, direction.x);
                for (float distance = 0f; distance <= length; distance += size.ObstacleSpacing)
                {
                    foreach (float side in new[] { -1f, 1f })
                    {
                        float scale = PickScale(size, random);
                        Vector3 position = start + direction * distance +
                            normal * side * (size.RoadWidth * 0.5f + GetHalfFootprint(scale));
                        TryAdd(position, scale, size, stations, nodes, roads, random, obstacles);
                    }
                }
            }
        }

        private void TryAdd(
            Vector3 position,
            float scale,
            MapSizeSettings size,
            IReadOnlyList<MapStation> stations,
            IReadOnlyList<MapNode> nodes,
            IReadOnlyList<MapRoad> roads,
            Random random,
            List<ObstacleSpot> obstacles)
        {
            position.y = 0f;
            float half = GetHalfFootprint(scale) - EDGE_TOLERANCE;
            Vector2 min = size.Bounds.Min;
            Vector2 max = size.Bounds.Max;
            if (position.x - half < min.x || position.x + half > max.x ||
                position.z - half < min.y || position.z + half > max.y)
            {
                return;
            }

            foreach (MapStation station in stations)
            {
                if (MapGeometry.Distance(position, station.Position) < station.Radius + _settings.ZoneClearance + half)
                {
                    return;
                }
            }

            foreach (MapNode node in nodes)
            {
                if (MapGeometry.Distance(position, node.Center) < node.Radius + _settings.ZoneClearance + half)
                {
                    return;
                }
            }

            foreach (MapRoad road in roads)
            {
                if (MapGeometry.DistanceToPolyline(position, road.Points) < size.RoadWidth * 0.5f + half)
                {
                    return;
                }
            }

            float minimumSpacing = size.ObstacleSpacing * MIN_SPACING_SHARE;
            foreach (ObstacleSpot obstacle in obstacles)
            {
                if (MapGeometry.Distance(position, obstacle.Position) < minimumSpacing)
                {
                    return;
                }
            }

            obstacles.Add(new ObstacleSpot(position, (float)random.NextDouble() * 360f, scale));
        }

        private float PickScale(MapSizeSettings size, Random random)
        {
            return Mathf.Lerp(size.ObstacleScale.Min, size.ObstacleScale.Max, (float)random.NextDouble());
        }

        private float GetHalfFootprint(float scale)
        {
            return _settings.ObstacleFootprint * scale * 0.5f;
        }
    }
}
