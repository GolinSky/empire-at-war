using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.Map.Hazards;
using EmpireAtWar.Utils;
using UnityEngine;
using Random = System.Random;

namespace EmpireAtWar.Entities.Map.Generation
{
    public static class MapDecorationPlacer
    {
        private const int ATTEMPTS_PER_PROP = 128;

        public static IReadOnlyList<MapDecorationSpot> Place(PlanetDecorationSet set, int seed,
            MapSizeSettings size, IReadOnlyList<MapStation> stations, IReadOnlyList<MapNode> nodes,
            IReadOnlyList<MapLane> lanes, IReadOnlyList<AsteroidField> fields,
            IReadOnlyList<HazardShape> hazards, float nodeClearance)
        {
            List<MapDecorationSpot> result = new List<MapDecorationSpot>();
            if (set == null) return result;
            Random random = new Random(seed);
            Vector2 center = (size.Bounds.Min + size.Bounds.Max) * 0.5f;
            Vector2 halfSize = (size.Bounds.Max - size.Bounds.Min) * (set.PlacementBoundsFraction * 0.5f);
            foreach (PlanetDecorationEntry entry in set.Entries)
            {
                if (entry.Prefab == null || entry.MinimumCount < 0 || entry.MaximumCount < entry.MinimumCount ||
                    entry.MinimumScale <= 0f || entry.MaximumScale < entry.MinimumScale || entry.ClearanceRadius <= 0f)
                    throw new InvalidOperationException($"Invalid planet decoration entry in {set.name}.");
                int count = random.Next(entry.MinimumCount, entry.MaximumCount + 1);
                for (int i = 0; i < count; i++)
                {
                    float scale = Mathf.Lerp(entry.MinimumScale, entry.MaximumScale, (float)random.NextDouble());
                    float radius = entry.ClearanceRadius * scale;
                    Vector2 min = center - halfSize + Vector2.one * radius;
                    Vector2 max = center + halfSize - Vector2.one * radius;
                    if (min.x > max.x || min.y > max.y) continue;
                    for (int attempt = 0; attempt < ATTEMPTS_PER_PROP; attempt++)
                    {
                        Vector3 position = new Vector3(Mathf.Lerp(min.x, max.x, (float)random.NextDouble()), 0f,
                            Mathf.Lerp(min.y, max.y, (float)random.NextDouble()));
                        if (!HasClearance(position, radius, size.MinLaneWidth, stations, nodes, lanes,
                                fields, hazards, result, nodeClearance)) continue;
                        position.y = ((float)random.NextDouble() * 2f - 1f) * entry.HeightJitter;
                        result.Add(new MapDecorationSpot(entry.Prefab, position,
                            (float)random.NextDouble() * 360f, scale, radius));
                        break;
                    }
                }
            }
            return result;
        }

        public static bool HasClearance(Vector3 position, float radius, float minLaneWidth,
            IReadOnlyList<MapStation> stations, IReadOnlyList<MapNode> nodes, IReadOnlyList<MapLane> lanes,
            IReadOnlyList<AsteroidField> fields, IReadOnlyList<HazardShape> hazards,
            IReadOnlyList<MapDecorationSpot> decorations, float nodeClearance)
        {
            foreach (MapStation station in stations)
                if (PlanarGeometry.Distance(position, station.Position) < radius + station.Radius + nodeClearance) return false;
            foreach (MapNode node in nodes)
                if (PlanarGeometry.Distance(position, node.Center) < radius + node.Radius + nodeClearance) return false;
            foreach (MapLane lane in lanes)
                if (PlanarGeometry.DistanceToPolyline(position, lane.Points) < radius + minLaneWidth * 0.5f) return false;
            foreach (AsteroidField field in fields)
            foreach (FieldVolume volume in field.Volumes)
                if (PlanarGeometry.Distance(position, volume.Center) < radius + volume.Radius) return false;
            foreach (HazardShape hazard in hazards)
                if (hazard.SignedDistance(position) <= radius + hazard.CellSize) return false;
            foreach (MapDecorationSpot decoration in decorations)
                if (PlanarGeometry.Distance(position, decoration.Position) < radius + decoration.Radius) return false;
            return true;
        }
    }
}
