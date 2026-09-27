using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Models.Factions;
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
        private readonly RoadNetworkBuilder _roadNetworkBuilder;
        private readonly ObstacleWallBuilder _obstacleWallBuilder;

        public MapLayoutGenerator(MapGenerationSettings settings, MapFeatureRadii radii)
        {
            _settings = settings;
            _stationPlacer = new StationPlacer(settings);
            _pointOfInterestPlacer = new PointOfInterestPlacer(settings, radii);
            _roadNetworkBuilder = new RoadNetworkBuilder(settings);
            _obstacleWallBuilder = new ObstacleWallBuilder(settings);
        }

        public MapLayout Generate(
            MapSize mapSize,
            EnemyAiDifficulty difficulty,
            FactionType playerFaction,
            FactionType opponentFaction,
            Random random)
        {
            MapSizeSettings size = _settings.GetSize(mapSize);
            float difficultyShare = Mathf.InverseLerp(
                (int)EnemyAiDifficulty.Easy, (int)EnemyAiDifficulty.UltraHard, (int)difficulty);
            List<MapNode> nodes = new List<MapNode>();
            for (int attempt = 0; attempt < MAX_LAYOUT_ATTEMPTS; attempt++)
            {
                MapStation[] stations = _stationPlacer.Place(size.Bounds, playerFaction, opponentFaction, random);
                if (!_pointOfInterestPlacer.TryPlace(size, stations, difficultyShare, random, nodes))
                {
                    continue;
                }

                List<MapRoad> roads = _roadNetworkBuilder.Build(nodes, stations, random);
                return new MapLayout(
                    size.Bounds,
                    new Dictionary<FactionType, Vector3>
                    {
                        { stations[0].Faction, stations[0].Position },
                        { stations[1].Faction, stations[1].Position }
                    },
                    _pointOfInterestPlacer.PlacePlanet(size, random),
                    CreateZones(nodes),
                    CreateSites(nodes),
                    roads,
                    _obstacleWallBuilder.Build(size, stations, nodes, roads, random));
            }

            throw new InvalidOperationException(
                $"{mapSize} map settings leave no room for its zones and sites after {MAX_LAYOUT_ATTEMPTS} attempts.");
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
