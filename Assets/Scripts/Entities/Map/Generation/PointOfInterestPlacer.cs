using System.Collections.Generic;
using EmpireAtWar.Utils;
using EmpireAtWar.Models.Players;
using UnityEngine;
using Random = System.Random;

namespace EmpireAtWar.Entities.Map.Generation
{
    /// <summary>
    /// Places default zones beside the stations, the home mining point behind each station, and the remaining
    /// zones and sites as point-symmetric pairs so neither side gets the better map half. Every mining point
    /// is a cluster of <see cref="SITES_PER_MINING_POINT"/> neighbouring mining sites.
    /// </summary>
    public sealed class PointOfInterestPlacer
    {
        public const int SITES_PER_MINING_POINT = 3;
        // Neighbouring sites of one mining point sit at this share of the spacing that keeps their capture ranges
        // apart, so the cluster reads as one point; their ranges overlap.
        public const float MINING_SITE_SPACING_SHARE = 0.5f;

        private const int MAX_PAIR_ATTEMPTS = 400;
        private const int CANDIDATES_PER_PICK = 12;

        private const float MIRROR_TOLERANCE = 1f;

        // Free gap between neighbouring sites of one mining point and between the home point and its station.
        private const float MINING_SITE_GAP = 15f;
        private const float FULL_TURN_DEGREES = 360f;
        // A region half as wide as the map side spans the whole map.
        private const float WHOLE_MAP_SHARE = 0.5f;

        private readonly MapGenerationSettings _settings;

        private readonly MapFeatureRadii _radii;
        // Center distance between neighbouring sites of one mining point.
        private readonly float _miningSiteSpacing;
        // Distance from a mining point's center to each site of its equilateral site triangle.
        private readonly float _miningSiteSpread;

        public PointOfInterestPlacer(MapGenerationSettings settings, MapFeatureRadii radii)
        {
            _settings = settings;
            _radii = radii;
            _miningSiteSpacing = (2f * radii.MiningSite + MINING_SITE_GAP) * MINING_SITE_SPACING_SHARE;
            _miningSiteSpread = _miningSiteSpacing / Mathf.Sqrt(3f);
        }

        /// <returns>False when this random draw left no room; the caller retries with new draws.</returns>
        public bool TryPlace(
            MapSizeSettings size,
            IReadOnlyList<MapStation> stations,
            Random random,
            List<MapNode> nodes)
        {
            nodes.Clear();
            Vector3 center = PlanarGeometry.GetCenter(size.Bounds);
            AddDefaultZones(size, stations, center, nodes);
            if (!TryAddHomeMining(size, stations, center, nodes))
            {
                return false;
            }

            if (size.CapturableZoneCount % 2 == 1)
            {
                MapNode centralZone = new MapNode(
                    kind: MapNodeKind.CapturableZone, center: center, radius: _radii.Zone, owner: PlayerId.None);
                if (!IsClear(center, GetOuterRadius(centralZone, size), size, stations, nodes))
                {
                    return false;
                }

                centralZone.Mirror = nodes.Count;
                nodes.Add(centralZone);
            }

            // Each side of a duel owns one home point; the rest are mirrored pairs.
            int miningPairCount = (size.MiningPointCount - 2) / 2;
            // Contested central mines first, then the largest pockets (reinforcement zones) while room remains.
            return TryAddMiningPairs(size.CentralMiningPairCount, _settings.CentralRegionShare,
                    size, stations, center, random, nodes) &&
                TryAddPairs(MapNodeKind.CapturableZone, size.CapturableZoneCount / 2, WHOLE_MAP_SHARE,
                    size, stations, center, random, nodes) &&
                TryAddMiningPairs(miningPairCount - size.CentralMiningPairCount, WHOLE_MAP_SHARE,
                    size, stations, center, random, nodes) &&
                TryAddPairs(MapNodeKind.BattleSite, size.BattleSiteCount / 2, WHOLE_MAP_SHARE,
                    size, stations, center, random, nodes);
        }

        public Vector3 PlacePlanet(MapSizeSettings size, Random random)
        {
            Vector2 min = size.Bounds.Min;
            Vector2 max = size.Bounds.Max;
            Vector2 inset = (max - min) * _settings.PlanetBorderInset;
            // The tilted battle camera sees more of the planet in the forward half of the map.
            return new Vector3(
                Mathf.Lerp(min.x + inset.x, max.x - inset.x, (float)random.NextDouble()),
                0f,
                Mathf.Lerp((min.y + max.y) * 0.5f, max.y - inset.y, (float)random.NextDouble()));
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

        private void AddDefaultZones(
            MapSizeSettings size, IReadOnlyList<MapStation> stations, Vector3 center, List<MapNode> nodes)
        {
            foreach (MapStation station in stations)
            {
                // The zone sits beside its station on the side facing the map center.
                float side = station.Position.x < center.x ? 1f : -1f;
                float offset = station.Radius + _radii.Zone + _settings.ZoneClearance + size.DefaultZoneGap;
                Vector3 position = new Vector3(
                    station.Position.x + side * offset,
                    0f,
                    station.Position.z);
                nodes.Add(new MapNode(kind: MapNodeKind.DefaultZone, center: position, radius: _radii.Zone, owner: station.Owner));
            }

            PairWithMirroredStations(stations, center, 0, nodes);
        }

        /// <summary>
        /// Fans each station's home sites along an arc behind it, on the side facing its map corner,
        /// as close to the station footprint as the site clearance allows.
        /// </summary>
        private bool TryAddHomeMining(
            MapSizeSettings size,
            IReadOnlyList<MapStation> stations,
            Vector3 center,
            List<MapNode> nodes)
        {
            // Home sites reserve no pocket room: their pocket may spill past the border, and the station
            // clearing keeps its rocks off the footprint.
            float outer = GetOuterRadius(_radii.MiningSite, false, size);
            // Sites of one point sit closer than the clearance between points, so only the default zones are checked.
            List<MapNode> defaultZones = nodes.GetRange(0, nodes.Count);
            for (int site = 0; site < SITES_PER_MINING_POINT; site++)
            {
                int firstIndex = nodes.Count;
                foreach (MapStation station in stations)
                {
                    float distance = station.Radius + outer + MINING_SITE_GAP;
                    // Neighbouring sites on the arc keep the same gap as the sites of a contested point.
                    float step = 2f * Mathf.Asin(_miningSiteSpacing * 0.5f / distance) * Mathf.Rad2Deg;
                    Vector3 corner = new Vector3(
                        Mathf.Sign(station.Position.x - center.x), 0f, Mathf.Sign(station.Position.z - center.z)).normalized;
                    // A rotation commutes with point reflection, so mirrored stations get mirrored arcs.
                    Vector3 direction = Quaternion.Euler(0f, (site - 1) * step, 0f) * corner;
                    Vector3 position = new Vector3(station.Position.x, 0f, station.Position.z) + direction * distance;
                    if (!IsClear(position, outer, size, stations, defaultZones))
                    {
                        return false;
                    }

                    nodes.Add(new MapNode(kind: MapNodeKind.MiningSite, center: position, radius: _radii.MiningSite, owner: PlayerId.None));
                }

                PairWithMirroredStations(stations, center, firstIndex, nodes);
            }

            return true;
        }

        /// <summary>
        /// Links the per-station nodes starting at <paramref name="firstIndex"/> the same way point
        /// symmetry links their stations. A station without a reflected twin (three players) mirrors itself.
        /// </summary>
        private static void PairWithMirroredStations(
            IReadOnlyList<MapStation> stations,
            Vector3 center,
            int firstIndex,
            List<MapNode> nodes)
        {
            for (int i = 0; i < stations.Count; i++)
            {
                nodes[firstIndex + i].Mirror = firstIndex + FindMirroredStation(stations, center, i);
            }
        }

        private static int FindMirroredStation(IReadOnlyList<MapStation> stations, Vector3 center, int index)
        {
            Vector3 reflected = PlanarGeometry.Reflect(stations[index].Position, center);
            for (int i = 0; i < stations.Count; i++)
            {
                if (i != index && PlanarGeometry.Distance(stations[i].Position, reflected) < MIRROR_TOLERANCE)
                {
                    return i;
                }
            }

            return index;
        }

        /// <summary>Adds mirrored mining points, each a randomly turned triangle of neighbouring sites.</summary>
        private bool TryAddMiningPairs(
            int pairCount,
            float regionShare,
            MapSizeSettings size,
            IReadOnlyList<MapStation> stations,
            Vector3 center,
            Random random,
            List<MapNode> nodes)
        {
            float outer = GetOuterRadius(_miningSiteSpread + _radii.MiningSite, true, size);
            for (int pair = 0; pair < pairCount; pair++)
            {
                if (!TryPickPairPosition(outer, regionShare, size, stations, center, random, nodes, out Vector3 position))
                {
                    return false;
                }

                float turn = (float)random.NextDouble() * FULL_TURN_DEGREES;
                int index = nodes.Count;
                for (int site = 0; site < SITES_PER_MINING_POINT; site++)
                {
                    Vector3 offset = Quaternion.Euler(0f, turn + site * FULL_TURN_DEGREES / SITES_PER_MINING_POINT, 0f) *
                        Vector3.forward * _miningSiteSpread;
                    nodes.Add(new MapNode(kind: MapNodeKind.MiningSite, center: position + offset, radius: _radii.MiningSite, owner: PlayerId.None)
                        { Mirror = index + SITES_PER_MINING_POINT + site });
                }

                for (int site = 0; site < SITES_PER_MINING_POINT; site++)
                {
                    nodes.Add(new MapNode(kind: MapNodeKind.MiningSite, center: PlanarGeometry.Reflect(nodes[index + site].Center, center),
                        radius: _radii.MiningSite, owner: PlayerId.None) { Mirror = index + site });
                }
            }

            return true;
        }

        private bool TryAddPairs(
            MapNodeKind kind,
            int pairCount,
            float regionShare,
            MapSizeSettings size,
            IReadOnlyList<MapStation> stations,
            Vector3 center,
            Random random,
            List<MapNode> nodes)
        {
            float radius = kind == MapNodeKind.BattleSite ? _radii.BattleSite : _radii.Zone;
            float outer = GetOuterRadius(radius, true, size);
            for (int pair = 0; pair < pairCount; pair++)
            {
                if (!TryPickPairPosition(outer, regionShare, size, stations, center, random, nodes, out Vector3 position))
                {
                    return false;
                }

                int index = nodes.Count;
                nodes.Add(new MapNode(kind: kind, center: position, radius: radius, owner: PlayerId.None) { Mirror = index + 1 });
                nodes.Add(new MapNode(kind: kind, center: PlanarGeometry.Reflect(position, center), radius: radius, owner: PlayerId.None) { Mirror = index });
            }

            return true;
        }

        // Best-candidate sampling: of several valid spots, keep the one farthest from everything placed.
        private bool TryPickPairPosition(
            float outer,
            float regionShare,
            MapSizeSettings size,
            IReadOnlyList<MapStation> stations,
            Vector3 center,
            Random random,
            List<MapNode> nodes,
            out Vector3 position)
        {
            // Candidates stay inside the region about the center; the pocket must still fit the map.
            Vector2 regionHalfSize = (size.Bounds.Max - size.Bounds.Min) * regionShare;
            Vector2 middle = new Vector2(center.x, center.z);
            Vector2 min = Vector2.Max(size.Bounds.Min + Vector2.one * outer, middle - regionHalfSize);
            Vector2 max = Vector2.Min(size.Bounds.Max - Vector2.one * outer, middle + regionHalfSize);
            float bestScore = float.MinValue;
            int validCandidates = 0;
            position = default;
            for (int attempt = 0; attempt < MAX_PAIR_ATTEMPTS && validCandidates < CANDIDATES_PER_PICK; attempt++)
            {
                Vector3 candidate = new Vector3(
                    Mathf.Lerp(min.x, max.x, (float)random.NextDouble()),
                    0f,
                    Mathf.Lerp(min.y, max.y, (float)random.NextDouble()));
                Vector3 mirror = PlanarGeometry.Reflect(candidate, center);
                if (PlanarGeometry.Distance(candidate, mirror) < outer * 2f + size.PointSpacing ||
                    !IsClear(candidate, outer, size, stations, nodes) ||
                    !IsClear(mirror, outer, size, stations, nodes))
                {
                    continue;
                }

                validCandidates++;
                float score = PlanarGeometry.Distance(candidate, mirror);
                foreach (MapNode node in nodes)
                {
                    score = Mathf.Min(score, PlanarGeometry.Distance(candidate, node.Center));
                }

                foreach (MapStation station in stations)
                {
                    score = Mathf.Min(score, PlanarGeometry.Distance(candidate, station.Position) - station.Radius);
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
                if (PlanarGeometry.Distance(position, station.Position) < station.Radius + outer)
                {
                    return false;
                }
            }

            foreach (MapNode node in nodes)
            {
                // Point spacing leaves lane room between two asteroid pockets; default zones have none.
                float spacing = node.HasPocket ? size.PointSpacing : 0f;
                if (PlanarGeometry.Distance(position, node.Center) < outer + GetOuterRadius(node, size) + spacing)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
