using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Utils;
using UnityEngine;
using Random = System.Random;

namespace EmpireAtWar.Entities.Map.Generation
{
    /// <summary>Places the point-symmetric main relay group at the exact map center.</summary>
    public sealed class MainZonePlacer
    {
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
            List<MapNode> group = CreateGroup(center, stations.Count, (float)random.NextDouble() * FULL_TURN);
            return _placement.TryAddGroup(group, size, stations, nodes);
        }

        private List<MapNode> CreateGroup(Vector3 position, int pointId, float turn)
        {
            List<MapNode> group = new List<MapNode>
            {
                new MapNode(MapNodeKind.CapturableZone, position, PlayerId.None, _radii.MainZone, pointId)
                { Mirror = 0, IsMain = true }
            };
            AddRing(group, position, pointId, MapNodeKind.MiningSite, _settings.MainZoneMiningCount,
                _radii.MainZone * _settings.MainZoneMiningRadiusShare, _radii.MiningSite, turn);
            float satelliteDistance = _radii.MainZone + _radii.MiningSite +
                _settings.SatelliteFootprintRadius + _settings.ZoneClearance;
            AddRing(group, position, pointId, MapNodeKind.SatelliteSite, _settings.MainZoneSatelliteCount,
                satelliteDistance, _settings.SatelliteFootprintRadius, turn + FULL_TURN / 12f);
            return group;
        }

        private static void AddRing(List<MapNode> group, Vector3 center, int pointId, MapNodeKind kind,
            int count, float distance, float radius, float turn)
        {
            if (count < 2 || count % 2 != 0)
                throw new System.InvalidOperationException($"Main relay {kind} ring requires a positive even count; got {count}.");
            int first = group.Count;
            for (int i = 0; i < count; i++)
            {
                Vector3 offset = Quaternion.Euler(0f, turn + i * FULL_TURN / count, 0f) *
                    Vector3.forward * distance;
                group.Add(new MapNode(kind, center + offset, PlayerId.None, radius, pointId)
                { Mirror = first + (i + count / 2) % count });
            }
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
