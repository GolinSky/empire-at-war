using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Models.Players;
using UnityEngine;
using Random = System.Random;

namespace EmpireAtWar.Entities.Map.Generation
{
    /// <summary>Builds a random, point-symmetric skirmish battlefield for the chosen map size and difficulty.</summary>
    public sealed class MapLayoutGenerator
    {
        private const int MAX_LAYOUT_ATTEMPTS = 60;

        private readonly MapGenerationSettings _settings;
        private readonly StationPlacer _stationPlacer;
        private readonly PointOfInterestPlacer _pointOfInterestPlacer;
        private readonly LaneNetworkBuilder _laneNetworkBuilder;
        private readonly AsteroidFieldBuilder _asteroidFieldBuilder;

        public MapLayoutGenerator(MapGenerationSettings settings, MapFeatureRadii radii)
        {
            _settings = settings;
            _stationPlacer = new StationPlacer(settings);
            _pointOfInterestPlacer = new PointOfInterestPlacer(settings, radii);
            _laneNetworkBuilder = new LaneNetworkBuilder(settings);
            _asteroidFieldBuilder = new AsteroidFieldBuilder(settings);
        }

        public MapLayout Generate(
            MapSize mapSize,
            IReadOnlyList<PlayerSlot> players,
            Random random)
        {
            MapSizeSettings size = _settings.GetSize(mapSize);
            float difficultyShare = Mathf.InverseLerp(
                (int)EnemyAiDifficulty.Easy, (int)EnemyAiDifficulty.UltraHard, (int)GetHardestAi(players));
            List<MapNode> nodes = new List<MapNode>();
            for (int attempt = 0; attempt < MAX_LAYOUT_ATTEMPTS; attempt++)
            {
                MapStation[] stations = _stationPlacer.Place(size.Bounds, players, random);
                if (!_pointOfInterestPlacer.TryPlace(size, stations, difficultyShare, random, nodes))
                {
                    continue;
                }

                List<MapLane> lanes = _laneNetworkBuilder.Build(nodes, stations, random);
                return new MapLayout(
                    size.Bounds,
                    CreateStationPositions(stations),
                    _pointOfInterestPlacer.PlacePlanet(size, random),
                    CreateZones(nodes),
                    CreateSites(nodes),
                    lanes,
                    _asteroidFieldBuilder.Build(size, stations, nodes, lanes, random));
            }

            throw new InvalidOperationException(
                $"{mapSize} map settings leave no room for its zones and sites after {MAX_LAYOUT_ATTEMPTS} attempts.");
        }

        // Home mines sit farther from every station the harder the strongest AI is.
        private static EnemyAiDifficulty GetHardestAi(IReadOnlyList<PlayerSlot> players)
        {
            EnemyAiDifficulty hardest = EnemyAiDifficulty.Easy;
            foreach (PlayerSlot player in players)
            {
                if (player.IsAi && player.Difficulty > hardest)
                {
                    hardest = player.Difficulty;
                }
            }

            return hardest;
        }

        private static Dictionary<PlayerId, Vector3> CreateStationPositions(IReadOnlyList<MapStation> stations)
        {
            Dictionary<PlayerId, Vector3> positions = new Dictionary<PlayerId, Vector3>();
            foreach (MapStation station in stations)
            {
                positions.Add(station.Owner, station.Position);
            }

            return positions;
        }

        private static List<ZoneSpot> CreateZones(IReadOnlyList<MapNode> nodes)
        {
            List<ZoneSpot> zones = new List<ZoneSpot>();
            foreach (MapNode node in nodes)
            {
                if (node.Kind == MapNodeKind.DefaultZone || node.Kind == MapNodeKind.CapturableZone)
                {
                    zones.Add(new ZoneSpot(node.Center, node.Owner, node.Kind == MapNodeKind.CapturableZone));
                }
            }

            return zones;
        }

        private static List<SiteSpot> CreateSites(IReadOnlyList<MapNode> nodes)
        {
            List<SiteSpot> sites = new List<SiteSpot>();
            foreach (MapNode node in nodes)
            {
                if (node.Kind == MapNodeKind.MiningSite)
                {
                    sites.Add(new SiteSpot(node.Center, SiteFacilityType.Mining));
                }
                else if (node.Kind == MapNodeKind.BattleSite)
                {
                    sites.Add(new SiteSpot(node.Center, SiteFacilityType.BattleAsteroid));
                }
            }

            return sites;
        }
    }
}
