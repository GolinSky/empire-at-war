using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.Map.Hazards;
using EmpireAtWar.Models.Players;
using UnityEngine;
using Random = System.Random;

namespace EmpireAtWar.Entities.Map.Generation
{
    /// <summary>Builds a random, point-symmetric skirmish battlefield for the chosen map size and difficulty.</summary>
    public sealed class MapLayoutGenerator
    {
        private const int MAX_LAYOUT_ATTEMPTS = 60;
        private const int DECOR_SALT = 0x4445434F;

        private readonly MapGenerationSettings _settings;
        private readonly StationPlacer _stationPlacer;
        private readonly PointOfInterestPlacer _pointOfInterestPlacer;
        private readonly LaneNetworkBuilder _laneNetworkBuilder;
        private readonly AsteroidFieldBuilder _asteroidFieldBuilder;
        private readonly HazardPlacer _hazardPlacer;

        public MapLayoutGenerator(MapGenerationSettings settings, MapFeatureRadii radii)
        {
            _settings = settings;
            _stationPlacer = new StationPlacer(settings, radii);
            _pointOfInterestPlacer = new PointOfInterestPlacer(settings, radii);
            _laneNetworkBuilder = new LaneNetworkBuilder(settings);
            _asteroidFieldBuilder = new AsteroidFieldBuilder(settings);
            _hazardPlacer = new HazardPlacer(settings, _pointOfInterestPlacer);
        }

        public MapLayout Generate(
            MapSize mapSize,
            IReadOnlyList<PlayerSlot> players,
            Random random,
            int hazardSeed)
        {
            MapSizeSettings size = _settings.GetSize(mapSize);
            List<MapNode> nodes = new List<MapNode>();
            for (int attempt = 0; attempt < MAX_LAYOUT_ATTEMPTS; attempt++)
            {
                MapStation[] stations = _stationPlacer.Place(size, players, random);
                if (!_pointOfInterestPlacer.TryPlace(size, stations, players, random, nodes))
                {
                    continue;
                }

                List<MapLane> lanes = _laneNetworkBuilder.Build(nodes, stations, random);
                Vector3 planetPosition = _pointOfInterestPlacer.PlacePlanet(size, random);
                IReadOnlyList<HazardSpot> spots = _hazardPlacer.Place(size, stations, nodes, hazardSeed + attempt);
                if (_settings.Hazards.Count > 0 && spots.Count != size.HazardCount)
                    continue;
                HazardShape[] hazards = new HazardShape[spots.Count];
                for (int i = 0; i < hazards.Length; i++)
                    hazards[i] = new HazardShape(spots[i]);
                MainLineBlocker blocker = new MainLineBlocker(_settings);
                if (!blocker.TryReserve(size, stations, nodes, lanes, hazards, out List<FieldVolume> reserved))
                    continue;
                List<AsteroidField> fields = _asteroidFieldBuilder.Build(size, stations, nodes, lanes, random,
                    hazards, out FieldDensity density);
                blocker.Fill(size, nodes, hazards, reserved, fields, random);
                IReadOnlyList<AsteroidSpot> decor = new DecorAsteroidBuilder(_settings).Build(size, density,
                    new Random(hazardSeed ^ DECOR_SALT));
                return new MapLayout(
                    sizeRange: size.Bounds,
                    stationPositions: CreateStationPositions(stations),
                    planetPosition: planetPosition,
                    zones: CreateZones(nodes),
                    sites: CreateSites(nodes),
                    lanes: lanes,
                    fields: fields,
                    hazards: hazards,
                    decorRocks: decor,
                    edgeMargin: size.EdgeMargin);
            }

            throw new InvalidOperationException(
                $"{mapSize} map settings leave no room for its zones, sites and hazards after {MAX_LAYOUT_ATTEMPTS} attempts.");
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
                    zones.Add(new ZoneSpot(node.Center, node.Owner, node.Kind == MapNodeKind.CapturableZone,
                        node.IsMain));
                }
            }

            return zones;
        }

        private static List<SiteSpot> CreateSites(IReadOnlyList<MapNode> nodes)
        {
            List<SiteSpot> sites = new List<SiteSpot>();
            foreach (MapNode node in nodes)
            {
                switch (node.Kind)
                {
                    case MapNodeKind.MiningSite:
                        sites.Add(new SiteSpot(node.Center, CaptureSiteKind.Mining, node.Owner, node.PointId));
                        break;
                    case MapNodeKind.SatelliteSite:
                        sites.Add(new SiteSpot(node.Center, CaptureSiteKind.Satellite, node.Owner, node.PointId));
                        break;
                }
            }
            return sites;
        }
    }
}
