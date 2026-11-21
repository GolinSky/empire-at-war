using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.Map.Hazards;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Utils;
using UnityEngine;
using Random = System.Random;

namespace EmpireAtWar.Entities.Map.Generation
{
    /// <summary>Builds seeded archetype battlefields while retaining the legacy Open Field generation path.</summary>
    public sealed class MapLayoutGenerator
    {
        private const int MAX_LAYOUT_ATTEMPTS = 60;
        private const int DECOR_SALT = 0x4445434F;

        private readonly MapGenerationSettings _settings;
        private readonly MapFeatureRadii _radii;
        private readonly StationPlacer _stationPlacer;
        private readonly PointOfInterestPlacer _pointOfInterestPlacer;
        private readonly LaneNetworkBuilder _laneNetworkBuilder;
        private readonly AsteroidFieldBuilder _asteroidFieldBuilder;
        private readonly HazardPlacer _hazardPlacer;
        private readonly SealedSpaceFiller _sealedSpaceFiller;
        private readonly FreeSatellitePlacer _freeSatellitePlacer;

        public MapLayoutGenerator(MapGenerationSettings settings, MapFeatureRadii radii)
        {
            _settings = settings;
            _radii = radii;
            _stationPlacer = new StationPlacer(settings, radii);
            _pointOfInterestPlacer = new PointOfInterestPlacer(settings, radii);
            _laneNetworkBuilder = new LaneNetworkBuilder(settings);
            _asteroidFieldBuilder = new AsteroidFieldBuilder(settings);
            _hazardPlacer = new HazardPlacer(settings, _pointOfInterestPlacer);
            _sealedSpaceFiller = new SealedSpaceFiller(settings);
            _freeSatellitePlacer = new FreeSatellitePlacer(settings, _pointOfInterestPlacer);
        }

        public MapLayout Generate(
            MapSize mapSize,
            IReadOnlyList<PlayerSlot> players,
            Random random,
            int hazardSeed)
        {
            return GenerateLayout(mapSize, _settings.GetSize(mapSize), players, random, hazardSeed);
        }

        public MapLayout Generate(MapSize mapSize, IReadOnlyList<PlayerSlot> players, Random random,
            int hazardSeed, PlanetType planet)
        {
            if (ReferenceEquals(_settings.Profiles, null))
                throw new InvalidOperationException("Map generation requires a planet profile table.");
            PlanetMapProfileEntry profile = _settings.Profiles.Get(planet);
            int maximum = MatchRules.GetMaxPlayers(mapSize);
            if (players.Count < 2 || players.Count > maximum || players.Count > profile.Archetype.MaximumPlayers)
                throw new InvalidOperationException($"{planet} on {mapSize} does not support {players.Count} players.");
            MapSizeSettings size = _settings.GetSize(mapSize);
            if (profile.Archetype.Kind != MapArchetypeKind.OpenField || players.Count > 4)
                size = size.ForArchetype(profile, players.Count);
            if (size.IsArchetypeLayout && size.HazardCount > 0 && _settings.Hazards.Count == 0)
                throw new InvalidOperationException($"{planet} requires {size.HazardCount} hazards, but no hazard presets are configured.");
            size.ConfigureStructuralFields(_radii.MainZone, _settings.ZoneClearance);
            return GenerateLayout(mapSize, size, players, random, hazardSeed, profile.Decorations, profile.GetSeed());
        }

        private static bool HasUnevenTeams(IReadOnlyList<PlayerSlot> players)
        {
            var sizes = new Dictionary<TeamId, int>();
            foreach (PlayerSlot player in players)
            {
                sizes.TryGetValue(player.Team, out int count);
                sizes[player.Team] = count + 1;
            }
            int first = -1;
            foreach (int count in sizes.Values)
            {
                if (first < 0) first = count;
                else if (count != first) return true;
            }
            return false;
        }

        private MapLayout GenerateLayout(MapSize mapSize, MapSizeSettings size,
            IReadOnlyList<PlayerSlot> players, Random random, int hazardSeed,
            PlanetDecorationSet decorations = null, int planetSeed = 0)
        {
            List<MapNode> nodes = new List<MapNode>();
            string fairnessDetail = string.Empty;
            int stationFailures = 0, pointFailures = 0, obstacleFailures = 0, fairnessFailures = 0, hazardFailures = 0,
                freePadFailures = 0;
            bool hasAllies = false;
            for (int i = 0; i < players.Count; i++)
            for (int j = i + 1; j < players.Count; j++)
                hasAllies |= players[i].Team == players[j].Team;
            int attempts = MAX_LAYOUT_ATTEMPTS * (size.IsArchetypeLayout && hasAllies ? 2 : 1);
            // Small maps keep equal spacing and the team-swap fallback; larger maps group allies on every attempt.
            bool groupTeams = mapSize != MapSize.Small;
            bool unevenTeams = HasUnevenTeams(players);
            for (int attempt = 0; attempt < attempts; attempt++)
            {
                // Uneven rosters (2v1, 3v1, 2v1v1) fall back to equal spacing once grouped layouts kept failing.
                bool groupThisAttempt = groupTeams && !(unevenTeams && attempt >= MAX_LAYOUT_ATTEMPTS);
                // Twin Pass flanks ignore grouping, so its uneven fallback is the boundary swap instead.
                bool swapTeams = attempt >= MAX_LAYOUT_ATTEMPTS &&
                    (!groupTeams || unevenTeams && size.Archetype == MapArchetypeKind.TwinPass);
                MapStation[] stations = size.IsArchetypeLayout
                    ? _stationPlacer.PlaceSlots(size, players, random, size.PlanetSeed, groupThisAttempt, swapTeams)
                    : _stationPlacer.Place(size, players, random, groupThisAttempt);
                if (size.IsArchetypeLayout && !_stationPlacer.HasStationClearance(size, stations, players))
                {
                    stationFailures++;
                    continue;
                }
                bool stationOnWall = false;
                foreach (FieldVolume wall in size.StructuralFields)
                foreach (MapStation station in stations)
                    if (PlanarGeometry.Distance(station.Position, wall.Center) < wall.Radius + station.Radius + _settings.ZoneClearance)
                        stationOnWall = true;
                if (stationOnWall) { stationFailures++; continue; }
                if (!_pointOfInterestPlacer.TryPlace(size, stations, players, random, nodes))
                {
                    pointFailures++;
                    continue;
                }

                List<MapLane> lanes = _laneNetworkBuilder.Build(nodes, stations, random,
                    size.ExtraLaneChanceMultiplier, size.LaneBendMultiplier);
                if (size.IsArchetypeLayout)
                    AddMainAccessLanes(nodes, lanes);
                if (size.IsArchetypeLayout) ArchetypeLaneRouter.Route(size, lanes);
                Vector3 planetPosition = _pointOfInterestPlacer.PlacePlanet(size, random);
                List<FieldVolume> centralObstacle = new List<FieldVolume>();
                if (size.CentralObstacleRadius > 0f)
                {
                    planetPosition = PlanarGeometry.GetCenter(size.Bounds);
                    centralObstacle.Add(new FieldVolume(planetPosition, size.CentralObstacleRadius));
                    bool overlaps = false;
                    foreach (MapNode node in nodes)
                        if (Vector3.Distance(node.Center, planetPosition) < node.Radius +
                            size.CentralObstacleRadius + size.MinLaneWidth * 0.5f)
                            overlaps = true;
                    if (overlaps || !LaneObstacleRouter.TryRoute(size, lanes, centralObstacle))
                    {
                        obstacleFailures++;
                        continue;
                    }
                }
                if (size.StructuralFields.Count > 0 && !LaneObstacleRouter.TryRoute(size, lanes, size.StructuralFields))
                {
                    obstacleFailures++;
                    continue;
                }
                if (size.IsArchetypeLayout && !MapLayoutFairness.IsFair(stations, players, nodes, lanes,
                        out fairnessDetail, size.MaximumFairnessRatio))
                {
                    fairnessFailures++;
                    continue;
                }
                IReadOnlyList<HazardSpot> spots = _hazardPlacer.Place(size, stations, nodes, hazardSeed + attempt);
                if (_settings.Hazards.Count > 0 && spots.Count != size.HazardCount)
                {
                    hazardFailures++;
                    continue;
                }
                HazardShape[] hazards = new HazardShape[spots.Count];
                for (int i = 0; i < hazards.Length; i++)
                    hazards[i] = new HazardShape(spots[i]);
                MainLineBlocker blocker = new MainLineBlocker(_settings);
                List<FieldVolume> reserved = new List<FieldVolume>();
                if (!size.IsArchetypeLayout && !blocker.TryReserve(size, stations, nodes, lanes, hazards, out reserved))
                    continue;
                // Free pads join the nodes before fields grow, so fields, fills and decorations clear around them.
                if (!_freeSatellitePlacer.TryPlace(size, stations, nodes, lanes, hazards, reserved, hazardSeed))
                {
                    freePadFailures++;
                    continue;
                }
                List<AsteroidField> fields = _asteroidFieldBuilder.Build(size, stations, nodes, lanes, random,
                    hazards, out FieldDensity density);
                if (!size.IsArchetypeLayout)
                    blocker.Fill(size, nodes, hazards, reserved, fields, random);
                if (centralObstacle.Count > 0)
                    fields.Add(new AsteroidField(centralObstacle, Array.Empty<AsteroidSpot>(), -10000f, 10000f));
                _sealedSpaceFiller.Fill(size, stations, fields, random);
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
                    edgeMargin: size.EdgeMargin,
                    planetObstacleRadius: size.CentralObstacleRadius,
                    decorations: MapDecorationPlacer.Place(decorations, hazardSeed ^ planetSeed,
                        size, stations, nodes, lanes, fields, hazards, _settings.ZoneClearance));
            }

            throw new InvalidOperationException(
                $"{mapSize} {size.Archetype} {players.Count}-player seed {hazardSeed} layout failed after {attempts} attempts: " +
                $"stations={stationFailures}, points={pointFailures}, obstacle={obstacleFailures}, " +
                $"fairness={fairnessFailures}, hazards={hazardFailures}, freePads={freePadFailures}. " +
                $"Last points: {_pointOfInterestPlacer.LastFailure}. {fairnessDetail}");
        }

        private static void AddMainAccessLanes(IReadOnlyList<MapNode> nodes, List<MapLane> lanes)
        {
            Vector3 main = nodes[0].Center;
            foreach (MapNode node in nodes)
            {
                if (node.Kind != MapNodeKind.DefaultZone) continue;
                Vector3 direction = main - node.Center;
                Vector3 normal = new Vector3(-direction.z, 0f, direction.x);
                lanes.Add(new MapLane(new[] { node.Center, (node.Center + main) * 0.5f + normal * 0.12f, main }));
            }
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
