using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Utils;
using UnityEngine;
using Random = System.Random;

namespace EmpireAtWar.Entities.Map.Generation
{
    /// <summary>Samples a valid main relay group, preferring similar arrival distances for every team.</summary>
    public sealed class MainZonePlacer
    {
        private const int MAX_ATTEMPTS = 400;
        private const float FULL_TURN = 360f;
        private readonly MapGenerationSettings _settings;
        private readonly MapFeatureRadii _radii;
        private readonly PointOfInterestPlacer _placement;

        public MainZonePlacer(MapGenerationSettings settings, MapFeatureRadii radii, PointOfInterestPlacer placement)
        {
            _settings = settings;
            _radii = radii;
            _placement = placement;
        }

        public bool TryPlace(MapSizeSettings size, IReadOnlyList<MapStation> stations,
            IReadOnlyList<PlayerSlot> players, Random random, List<MapNode> nodes)
        {
            Vector3 center = PlanarGeometry.GetCenter(size.Bounds);
            Vector2 half = (size.Bounds.Max - size.Bounds.Min) * _settings.MainZoneRegionShare;
            List<MapNode> fallback = null;
            for (int attempt = 0; attempt < MAX_ATTEMPTS; attempt++)
            {
                Vector3 position = center + new Vector3(
                    ((float)random.NextDouble() * 2f - 1f) * half.x, 0f,
                    ((float)random.NextDouble() * 2f - 1f) * half.y);
                List<MapNode> group = CreateGroup(position, stations.Count, (float)random.NextDouble() * FULL_TURN);
                if (!_placement.TryAddGroup(group, size, stations, nodes))
                    continue;
                if (GetTeamDistanceSpread(position, stations, players) <= _settings.MainZoneFairnessTolerance)
                    return true;
                fallback = group;
                nodes.Clear();
            }
            if (fallback == null)
                return false;
            nodes.AddRange(fallback);
            return true;
        }

        private List<MapNode> CreateGroup(Vector3 position, int pointId, float turn)
        {
            List<MapNode> group = new List<MapNode>
            {
                new MapNode(MapNodeKind.CapturableZone, position, PlayerId.None, _radii.Zone, pointId)
            };
            AddRing(group, position, pointId, MapNodeKind.MiningSite, _settings.MainZoneMiningCount,
                _radii.Zone, _radii.MiningSite, turn);
            float satelliteDistance = _radii.Zone + _radii.MiningSite +
                _settings.SatelliteFootprintRadius + _settings.ZoneClearance;
            AddRing(group, position, pointId, MapNodeKind.SatelliteSite, _settings.MainZoneSatelliteCount,
                satelliteDistance, _settings.SatelliteFootprintRadius, turn + FULL_TURN / 12f);
            for (int i = 0; i < group.Count; i++)
                group[i].Mirror = i;
            return group;
        }

        private static void AddRing(List<MapNode> group, Vector3 center, int pointId, MapNodeKind kind,
            int count, float distance, float radius, float turn)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 offset = Quaternion.Euler(0f, turn + i * FULL_TURN / count, 0f) *
                    Vector3.forward * distance;
                group.Add(new MapNode(kind, center + offset, PlayerId.None, radius, pointId));
            }
        }

        private static float GetTeamDistanceSpread(Vector3 position, IReadOnlyList<MapStation> stations,
            IReadOnlyList<PlayerSlot> players)
        {
            Dictionary<TeamId, float> distances = new Dictionary<TeamId, float>();
            foreach (MapStation station in stations)
            {
                TeamId team = GetTeam(station.Owner, players);
                float distance = PlanarGeometry.Distance(position, station.Position);
                if (!distances.TryGetValue(team, out float nearest) || distance < nearest)
                    distances[team] = distance;
            }
            float min = float.MaxValue;
            float max = 0f;
            foreach (float distance in distances.Values)
            {
                min = Mathf.Min(min, distance);
                max = Mathf.Max(max, distance);
            }
            return max - min;
        }

        internal static TeamId GetNearestTeam(Vector3 position, IReadOnlyList<MapStation> stations,
            IReadOnlyList<PlayerSlot> players)
        {
            float nearest = float.MaxValue;
            PlayerId owner = PlayerId.None;
            foreach (MapStation station in stations)
            {
                float distance = PlanarGeometry.Distance(position, station.Position);
                if (distance < nearest)
                {
                    nearest = distance;
                    owner = station.Owner;
                }
            }
            return GetTeam(owner, players);
        }

        private static TeamId GetTeam(PlayerId owner, IReadOnlyList<PlayerSlot> players)
        {
            foreach (PlayerSlot player in players)
                if (player.Id == owner)
                    return player.Team;
            throw new System.InvalidOperationException($"No player slot exists for station {owner}.");
        }
    }
}
