using System.Collections.Generic;
using EmpireAtWar.Models.Factions;
using UnityEngine;
using Random = System.Random;

namespace EmpireAtWar.Entities.Map.Generation
{
    /// <summary>
    /// Places default zones beside the stations, one home mining site per side, and the remaining
    /// zones and sites as point-symmetric pairs so neither side gets the better map half.
    /// </summary>
    public sealed class PointOfInterestPlacer
    {
        private const int MAX_PAIR_ATTEMPTS = 400;
        private const int CANDIDATES_PER_PICK = 12;

        private readonly MapGenerationSettings _settings;
        private readonly MapFeatureRadii _radii;

        public PointOfInterestPlacer(MapGenerationSettings settings, MapFeatureRadii radii)
        {
            _settings = settings;
            _radii = radii;
        }

        /// <returns>False when this random draw left no room; the caller retries with new draws.</returns>
        public bool TryPlace(
            MapSizeSettings size,
            IReadOnlyList<MapStation> stations,
            float difficulty,
            Random random,
            List<MapNode> nodes)
        {
            nodes.Clear();
            Vector3 center = MapGeometry.GetCenter(size.Bounds);
            AddDefaultZones(stations, center, nodes);
            if (!TryAddHomeMining(size, stations, center, difficulty, random, nodes))
            {
                return false;
            }

            if (size.CapturableZoneCount % 2 == 1)
            {
                MapNode centralZone = new MapNode(
                    MapNodeKind.CapturableZone, center, _radii.Zone, PlayerType.None);
                if (!IsClear(center, GetOuterRadius(centralZone, size), size, stations, nodes))
                {
                    return false;
                }

                centralZone.Mirror = nodes.Count;
                nodes.Add(centralZone);
            }

            // Reserve the larger facility pockets before placing reinforcement zones.
            return TryAddPairs(MapNodeKind.MiningSite, (size.MiningSiteCount - 2) / 2,
                    size, stations, center, random, nodes) &&
                TryAddPairs(MapNodeKind.BattleSite, size.BattleSiteCount / 2,
                    size, stations, center, random, nodes) &&
                TryAddPairs(MapNodeKind.CapturableZone, size.CapturableZoneCount / 2,
                    size, stations, center, random, nodes);
        }

        public Vector3 PlacePlanet(MapSizeSettings size, Random random)
        {
            Vector2 min = size.Bounds.Min;
            Vector2 max = size.Bounds.Max;
            Vector2 inset = (max - min) * _settings.PlanetBorderInset;
            return new Vector3(
                Mathf.Lerp(min.x + inset.x, max.x - inset.x, (float)random.NextDouble()),
                0f,
                Mathf.Lerp(min.y + inset.y, max.y - inset.y, (float)random.NextDouble()));
        }

        /// <summary>Node radius plus the clearance and, for pocketed nodes, room for the asteroid pocket.</summary>
        public float GetOuterRadius(MapNode node, MapSizeSettings size)
        {
            return GetOuterRadius(node.Radius, node.HasPocket, size);
        }

        private float GetOuterRadius(float radius, bool hasPocket, MapSizeSettings size)
        {
            return radius + _settings.ZoneClearance + (hasPocket ? size.PocketThickness : 0f);
        }

        private void AddDefaultZones(IReadOnlyList<MapStation> stations, Vector3 center, List<MapNode> nodes)
        {
            foreach (MapStation station in stations)
            {
                // The zone sits beside its station on the side facing the map center.
                float side = station.Position.x < center.x ? 1f : -1f;
                Vector3 position = new Vector3(
                    station.Position.x + side * (station.Radius + _radii.Zone + _settings.ZoneClearance),
                    0f,
                    station.Position.z);
                nodes.Add(new MapNode(MapNodeKind.DefaultZone, position, _radii.Zone, station.Owner));
            }

            nodes[0].Mirror = 1;
            nodes[1].Mirror = 0;
        }

        private bool TryAddHomeMining(
            MapSizeSettings size,
            IReadOnlyList<MapStation> stations,
            Vector3 center,
            float difficulty,
            Random random,
            List<MapNode> nodes)
        {
            float gap = Mathf.Lerp(size.HomeMiningGap.Min, size.HomeMiningGap.Max, difficulty);
            float outer = GetOuterRadius(_radii.MiningSite, true, size);
            // One turn for both sides keeps the pair point-symmetric.
            float turn = (float)random.NextDouble() * _settings.HomeMiningAngle * Mathf.Deg2Rad;
            int firstIndex = nodes.Count;
            foreach (MapStation station in stations)
            {
                // The default zone takes the X side, so the mine heads along Z toward the
                // neighbouring corner and turns at most HomeMiningAngle toward the map center.
                float xSide = Mathf.Sign(center.x - station.Position.x);
                float zSide = Mathf.Sign(center.z - station.Position.z);
                Vector3 direction = new Vector3(xSide * Mathf.Sin(turn), 0f, zSide * Mathf.Cos(turn));
                Vector3 position = station.Position + direction * (station.Radius + outer + gap);
                // Thick asteroid pockets can outgrow the station's edge distance; pull the mine inside.
                position = new Vector3(
                    Mathf.Clamp(position.x, size.Bounds.Min.x + outer, size.Bounds.Max.x - outer),
                    0f,
                    Mathf.Clamp(position.z, size.Bounds.Min.y + outer, size.Bounds.Max.y - outer));
                if (!IsClear(position, outer, size, stations, nodes))
                {
                    return false;
                }

                nodes.Add(new MapNode(MapNodeKind.MiningSite, position, _radii.MiningSite, PlayerType.None));
            }

            nodes[firstIndex].Mirror = firstIndex + 1;
            nodes[firstIndex + 1].Mirror = firstIndex;
            return true;
        }

        private bool TryAddPairs(
            MapNodeKind kind,
            int pairCount,
            MapSizeSettings size,
            IReadOnlyList<MapStation> stations,
            Vector3 center,
            Random random,
            List<MapNode> nodes)
        {
            float radius = kind switch
            {
                MapNodeKind.MiningSite => _radii.MiningSite,
                MapNodeKind.BattleSite => _radii.BattleSite,
                _ => _radii.Zone
            };
            float outer = GetOuterRadius(radius, true, size);
            for (int pair = 0; pair < pairCount; pair++)
            {
                if (!TryPickPairPosition(outer, size, stations, center, random, nodes, out Vector3 position))
                {
                    return false;
                }

                int index = nodes.Count;
                nodes.Add(new MapNode(kind, position, radius, PlayerType.None) { Mirror = index + 1 });
                nodes.Add(new MapNode(kind, MapGeometry.Reflect(position, center), radius, PlayerType.None) { Mirror = index });
            }

            return true;
        }

        // Best-candidate sampling: of several valid spots, keep the one farthest from everything placed.
        private bool TryPickPairPosition(
            float outer,
            MapSizeSettings size,
            IReadOnlyList<MapStation> stations,
            Vector3 center,
            Random random,
            List<MapNode> nodes,
            out Vector3 position)
        {
            Vector2 min = size.Bounds.Min + Vector2.one * outer;
            Vector2 max = size.Bounds.Max - Vector2.one * outer;
            float bestScore = float.MinValue;
            int validCandidates = 0;
            position = default;
            for (int attempt = 0; attempt < MAX_PAIR_ATTEMPTS && validCandidates < CANDIDATES_PER_PICK; attempt++)
            {
                Vector3 candidate = new Vector3(
                    Mathf.Lerp(min.x, max.x, (float)random.NextDouble()),
                    0f,
                    Mathf.Lerp(min.y, max.y, (float)random.NextDouble()));
                Vector3 mirror = MapGeometry.Reflect(candidate, center);
                if (MapGeometry.Distance(candidate, mirror) < outer * 2f + size.PointSpacing ||
                    !IsClear(candidate, outer, size, stations, nodes) ||
                    !IsClear(mirror, outer, size, stations, nodes))
                {
                    continue;
                }

                validCandidates++;
                float score = MapGeometry.Distance(candidate, mirror);
                foreach (MapNode node in nodes)
                {
                    score = Mathf.Min(score, MapGeometry.Distance(candidate, node.Center));
                }

                foreach (MapStation station in stations)
                {
                    score = Mathf.Min(score, MapGeometry.Distance(candidate, station.Position) - station.Radius);
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    position = candidate;
                }
            }

            return validCandidates > 0;
        }

        private bool IsClear(
            Vector3 position,
            float outer,
            MapSizeSettings size,
            IReadOnlyList<MapStation> stations,
            IReadOnlyList<MapNode> nodes)
        {
            Vector2 min = size.Bounds.Min;
            Vector2 max = size.Bounds.Max;
            if (position.x - outer < min.x || position.x + outer > max.x ||
                position.z - outer < min.y || position.z + outer > max.y)
            {
                return false;
            }

            foreach (MapStation station in stations)
            {
                if (MapGeometry.Distance(position, station.Position) < station.Radius + outer)
                {
                    return false;
                }
            }

            foreach (MapNode node in nodes)
            {
                // Point spacing leaves lane room between two asteroid pockets; default zones have none.
                float spacing = node.HasPocket ? size.PointSpacing : 0f;
                if (MapGeometry.Distance(position, node.Center) < outer + GetOuterRadius(node, size) + spacing)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
