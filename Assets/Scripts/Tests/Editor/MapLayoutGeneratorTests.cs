using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmpireAtWar.Utils;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Map.Generation;
using EmpireAtWar.Entities.Map.Hazards;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Views.ReinforcementZones;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

namespace EmpireAtWar.Tests.Editor
{
    // Generating maps is slow, so the suite runs only when asked: MapGeneration checks a few seeds for map work,
    // MapExhaustive sweeps every seed before finishing a map-generation change (AGENTS.md Test Selection).
    [TestFixture(false, Category = "MapGeneration", Explicit = true)]
    [TestFixture(true, Category = "MapExhaustive", Explicit = true)]
    public sealed class MapLayoutGeneratorTests
    {
        private const string SETTINGS_PATH = "Assets/Settings/Data/Models/Map/MapGenerationSettings.asset";
        private const string ZONE_PREFAB_PATH = "Assets/Prefabs/View/ReinforcementZones/ReinforcementZone.prefab";
        private const string MAIN_ZONE_PREFAB_PATH = "Assets/Prefabs/View/ReinforcementZones/MainReinforcementZone.prefab";
        private const string MINING_PREFAB_PATH = "Assets/Prefabs/View/CaptureSites/CaptureSite.prefab";

        private const int SEED_COUNT = 20;
        private const int ROSTER_SEED_COUNT = 10;
        // Includes seeds that once broke lanes.
        private static readonly int[] QUICK_SEEDS = { 0, 7, 19 };

        private const float TOLERANCE = 0.01f;

        private MapGenerationSettings _settings;
        private MapFeatureRadii _radii;
        private MapLayoutGenerator _generator;
        // Generation is deterministic, so every test reading the same map shares one build.
        private Dictionary<string, MapLayout> _layouts;
        private readonly int[] _seeds;
        private readonly int[] _rosterSeeds;

        public MapLayoutGeneratorTests(bool exhaustive)
        {
            _seeds = exhaustive ? CreateSeedRange(SEED_COUNT) : QUICK_SEEDS;
            _rosterSeeds = exhaustive ? CreateSeedRange(ROSTER_SEED_COUNT) : QUICK_SEEDS;
        }

        private static int[] CreateSeedRange(int count)
        {
            int[] seeds = new int[count];
            for (int i = 0; i < count; i++)
                seeds[i] = i;
            return seeds;
        }

        [Test]
        public void RelayGroups_HaveExpectedCountsGeometryAndMirrorsAcrossRosters()
        {
            foreach (MapSize mapSize in Enum.GetValues(typeof(MapSize)))
            foreach (EnemyAiDifficulty difficulty in Enum.GetValues(typeof(EnemyAiDifficulty)))
            for (int playerCount = 2; playerCount <= 4; playerCount++)
            foreach (int seed in _seeds)
            {
                MapSizeSettings size = _settings.GetSize(mapSize);
                Random random = new Random(seed);
                PlayerSlot[] players = CreateRoster(playerCount, FactionType.Separatist, difficulty);
                MapStation[] stations = new StationPlacer(_settings, _radii).Place(size, players, random);
                PointOfInterestPlacer placer = new PointOfInterestPlacer(_settings, _radii);
                List<MapNode> nodes = new List<MapNode>();
                bool placed = false;
                for (int attempt = 0; attempt < 60 && !placed; attempt++)
                    placed = placer.TryPlace(size, stations, players, random, nodes);
                string context = $"{mapSize}, {difficulty}, {playerCount} players, seed {seed}";
                Assert.That(placed, Is.True, context);
                MapNode main = nodes[0];
                Assert.That(main.Kind, Is.EqualTo(MapNodeKind.CapturableZone), context);
                Vector3 center = PlanarGeometry.GetCenter(size.Bounds);
                Assert.That(main.IsMain, Is.True, context);
                Assert.That(main.Center, Is.EqualTo(center), context);
                Assert.That(main.Radius, Is.EqualTo(_radii.MainZone), context);
                float miningRadius = _radii.MainZone * _settings.MainZoneMiningRadiusShare;
                MapNode[] mainMines = nodes.Where(node => node.PointId == main.PointId &&
                    node.Kind == MapNodeKind.MiningSite).ToArray();
                MapNode[] mainPads = nodes.Where(node => node.PointId == main.PointId &&
                    node.Kind == MapNodeKind.SatelliteSite).ToArray();
                Assert.That(mainMines.Length, Is.EqualTo(4), context);
                Assert.That(mainPads.Length, Is.EqualTo(6), context);
                foreach (MapNode mine in mainMines)
                {
                    Assert.That(PlanarGeometry.Distance(mine.Center, main.Center),
                        Is.EqualTo(miningRadius).Within(TOLERANCE), context);
                    Assert.That(miningRadius + mine.Radius, Is.LessThan(_radii.MainZone), context);
                    float nearest = mainMines.Where(other => other != mine)
                        .Min(other => PlanarGeometry.Distance(mine.Center, other.Center));
                    Assert.That(nearest, Is.EqualTo(Mathf.Sqrt(2f) * miningRadius).Within(TOLERANCE), context);
                    Assert.That(nearest, Is.GreaterThan(2f * mine.Radius), context);
                }
                foreach (MapNode pad in mainPads)
                {
                    float radius = PlanarGeometry.Distance(pad.Center, main.Center);
                    Assert.That(radius - pad.Radius, Is.GreaterThanOrEqualTo(_radii.MainZone - TOLERANCE), context);
                    float nearest = mainPads.Where(other => other != pad)
                        .Min(other => PlanarGeometry.Distance(pad.Center, other.Center));
                    Assert.That(nearest, Is.EqualTo(radius).Within(TOLERANCE), context);
                }
                MapNode[] additional = nodes.Where(node => node.Kind == MapNodeKind.CapturableZone &&
                    node != main).ToArray();
                Assert.That(additional.Length, Is.EqualTo(2 * size.AdditionalZonePairCount), context);
                foreach (MapNode zone in additional)
                {
                    Assert.That(zone.IsMain, Is.False, context);
                    Assert.That(zone.Radius, Is.EqualTo(_radii.Zone), context);
                    float edgeDistance = Mathf.Min(zone.Center.x - size.Bounds.Min.x,
                        size.Bounds.Max.x - zone.Center.x, zone.Center.z - size.Bounds.Min.y,
                        size.Bounds.Max.y - zone.Center.z);
                    Assert.That(edgeDistance, Is.LessThanOrEqualTo(size.AdditionalZoneEdgeBand), context);
                    foreach (MapStation station in stations)
                        Assert.That(PlanarGeometry.Distance(zone.Center, station.Position) + TOLERANCE,
                            Is.GreaterThanOrEqualTo(size.AdditionalZoneStationDistance), context);
                    Assert.That(PlanarGeometry.Distance(zone.Center, main.Center) + TOLERANCE,
                        Is.GreaterThanOrEqualTo(size.AdditionalZoneMainDistance), context);
                    Assert.That(nodes.Count(node => node.PointId == zone.PointId &&
                        node.Kind == MapNodeKind.MiningSite), Is.EqualTo(1), context);
                    Assert.That(nodes.Count(node => node.PointId == zone.PointId &&
                        node.Kind == MapNodeKind.SatelliteSite), Is.EqualTo(2), context);
                    Assert.That(GetNearestTeam(zone.Center, stations, players),
                        Is.Not.EqualTo(GetNearestTeam(nodes[zone.Mirror].Center, stations, players)), context);
                }
                MapNode[] homeMines = nodes.Where(node => node.Kind == MapNodeKind.MiningSite &&
                    node.PointId != main.PointId && !additional.Any(zone => zone.PointId == node.PointId)).ToArray();
                Assert.That(homeMines.Length, Is.EqualTo(stations.Length), context);
                foreach (MapNode mine in homeMines)
                    Assert.That(nodes.Count(node => node.PointId == mine.PointId &&
                        node.Kind == MapNodeKind.SatelliteSite), Is.EqualTo(1), context);
                HashSet<MapNode> homeForStations = new HashSet<MapNode>();
                foreach (MapStation station in stations)
                {
                    Assert.That(nodes.Count(node => node.Kind == MapNodeKind.DefaultZone &&
                        node.Owner == station.Owner), Is.EqualTo(1), context);
                    MapNode homeMine = homeMines.OrderBy(mine =>
                        PlanarGeometry.Distance(mine.Center, station.Position)).First();
                    Assert.That(homeForStations.Add(homeMine), Is.True, context);
                    foreach (MapNode home in nodes.Where(node => node.PointId == homeMine.PointId))
                    {
                        Assert.That(home.IsHomeSite, Is.True, context);
                        Assert.That(home.HasPocket, Is.False, context);
                        Assert.That(PlanarGeometry.Distance(home.Center, station.Position),
                            Is.EqualTo(station.Radius + home.Radius + 15f).Within(TOLERANCE), context);
                        Assert.That(home.Center.x - home.Radius, Is.GreaterThanOrEqualTo(size.Bounds.Min.x), context);
                        Assert.That(home.Center.x + home.Radius, Is.LessThanOrEqualTo(size.Bounds.Max.x), context);
                        Assert.That(home.Center.z - home.Radius, Is.GreaterThanOrEqualTo(size.Bounds.Min.y), context);
                        Assert.That(home.Center.z + home.Radius, Is.LessThanOrEqualTo(size.Bounds.Max.y), context);
                    }
                }
                for (int i = 0; i < nodes.Count; i++)
                {
                    MapNode node = nodes[i];
                    Assert.That(node.Mirror, Is.InRange(0, nodes.Count - 1), context);
                    if (node.PointId == main.PointId && node != main)
                        Assert.That(node.Mirror, Is.Not.EqualTo(i), context);
                    if (node.Mirror != i)
                    {
                        Assert.That(nodes[node.Mirror].Mirror, Is.EqualTo(i), context);
                        Assert.That(nodes[node.Mirror].Kind, Is.EqualTo(node.Kind), context);
                        Assert.That(PlanarGeometry.Distance(nodes[node.Mirror].Center,
                            PlanarGeometry.Reflect(node.Center, center)), Is.LessThan(TOLERANCE), context);
                    }
                    if (node.Kind != MapNodeKind.SatelliteSite) continue;
                    Assert.That(node.Owner, Is.EqualTo(PlayerId.None), context);
                    foreach (MapNode other in nodes)
                        if (other != node && other.PointId != node.PointId)
                            Assert.That(PlanarGeometry.Distance(node.Center, other.Center) + TOLERANCE,
                                Is.GreaterThanOrEqualTo(node.Radius + other.Radius), context);
                    foreach (MapStation station in stations)
                        Assert.That(PlanarGeometry.Distance(node.Center, station.Position) + TOLERANCE,
                            Is.GreaterThanOrEqualTo(node.Radius + station.Radius +
                                (node.IsHomeSite ? 15f : _settings.ZoneClearance)), context);
                }
            }
        }

        [Test, Category("Capture")]
        public void MainRelayPrefab_UsesLargerRangeWithoutChangingOrdinaryRelays()
        {
            ReinforcementZoneView ordinary = AssetDatabase.LoadAssetAtPath<ReinforcementZoneView>(ZONE_PREFAB_PATH);
            ReinforcementZoneView main = AssetDatabase.LoadAssetAtPath<ReinforcementZoneView>(MAIN_ZONE_PREFAB_PATH);
            Assert.That(ordinary, Is.Not.Null);
            Assert.That(main, Is.Not.Null);
            Assert.That(ordinary.Radius, Is.EqualTo(270f));
            Assert.That(main.Radius, Is.EqualTo(540f));
            Assert.That(PrefabUtility.GetPrefabAssetType(main.gameObject), Is.EqualTo(PrefabAssetType.Variant));
            Assert.That(_radii.Zone, Is.EqualTo(ordinary.Radius));
            Assert.That(_radii.MainZone, Is.EqualTo(main.Radius));
        }

        [TestCase(MapSize.Small)]
        [TestCase(MapSize.Medium)]
        [TestCase(MapSize.Large)]
        public void MainRelay_ToEveryAdditionalZone_HasBlockedStraightLine(MapSize mapSize)
        {
            MapSizeSettings size = _settings.GetSize(mapSize);
            for (int playerCount = 2; playerCount <= 4; playerCount++)
            foreach (int seed in _seeds)
            {
                PlayerSlot[] players = CreateRoster(playerCount, FactionType.Separatist, EnemyAiDifficulty.Medium);
                MapLayout layout = GetLayout(mapSize, players, seed);
                ZoneSpot main = layout.Zones.Single(zone => zone.IsMain);
                Assert.That(main.Center, Is.EqualTo(PlanarGeometry.GetCenter(size.Bounds)));
                foreach (ZoneSpot zone in layout.Zones.Where(zone => zone.IsCapturable && !zone.IsMain))
                {
                    Vector3 direction = (zone.Center - main.Center).normalized;
                    float mainPocket = _radii.MainZone + _settings.ZoneClearance + size.PocketThickness;
                    float zonePocket = _radii.Zone + _settings.ZoneClearance + size.PocketThickness;
                    Assert.That(PlanarGeometry.Distance(main.Center, zone.Center),
                        Is.GreaterThan(mainPocket + zonePocket), "The relay pockets must leave a route between them.");
                    // Endpoint pocket rings cannot satisfy the requirement to obstruct the route between relays.
                    Vector3 routeStart = main.Center + direction * mainPocket;
                    Vector3 routeEnd = zone.Center - direction * zonePocket;
                    Vector2 start = new Vector2(routeStart.x, routeStart.z);
                    Vector2 end = new Vector2(routeEnd.x, routeEnd.z);
                    Vector3 across = new Vector3(-direction.z, 0f, direction.x);
                    bool blocked = layout.Fields.Any(field =>
                        field.Volumes.Max(volume => Vector3.Dot(volume.Center, across) + volume.Radius) -
                        field.Volumes.Min(volume => Vector3.Dot(volume.Center, across) - volume.Radius) >= size.LaneWidth &&
                        field.Volumes.Any(volume => PlanarGeometry.DistanceToSegmentSquared(
                            new Vector2(volume.Center.x, volume.Center.z), start, end) <= volume.Radius * volume.Radius));
                    int steps = Mathf.CeilToInt(PlanarGeometry.Distance(routeStart, routeEnd) /
                        (size.FieldCellSize * 0.5f));
                    for (int i = 0; i <= steps && !blocked; i++)
                    {
                        Vector3 sample = Vector3.Lerp(routeStart, routeEnd, i / (float)steps);
                        blocked = layout.Hazards.Any(hazard => hazard.Contains(sample, 0f, false) &&
                            Mathf.Abs(across.x) * hazard.Bounds.size.x + Mathf.Abs(across.z) * hazard.Bounds.size.z >= size.LaneWidth);
                    }
                    Assert.That(blocked, Is.True,
                        $"{mapSize}, {playerCount} players, seed {seed}: straight route to {zone.Center} is clear.");
                }
            }
        }

        [TestCase("<MainZoneMiningCount>k__BackingField", 3)]
        [TestCase("<MainZoneSatelliteCount>k__BackingField", 5)]
        [TestCase("<MainZoneMiningCount>k__BackingField", 0)]
        public void MainRelay_RejectsRingCountsThatCannotMirror(string property, int count)
        {
            MapGenerationSettings settings = UnityEngine.Object.Instantiate(_settings);
            try
            {
                SerializedObject serialized = new SerializedObject(settings);
                serialized.FindProperty(property).intValue = count;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                MainZonePlacer placer = new MainZonePlacer(settings, _radii,
                    new PointOfInterestPlacer(settings, _radii));
                InvalidOperationException error = Assert.Throws<InvalidOperationException>(() =>
                    placer.TryPlace(settings.GetSize(MapSize.Small), Array.Empty<MapStation>(),
                        Array.Empty<PlayerSlot>(), new Random(0), new List<MapNode>()));
                Assert.That(error.Message, Does.Contain("even count"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [TestCase(MapSize.Small)]
        [TestCase(MapSize.Medium)]
        [TestCase(MapSize.Large)]
        public void MainRelayBlocker_HasLaneScaleWidthDenseRocksAndPassableMirroredDetours(MapSize mapSize)
        {
            MapSizeSettings size = _settings.GetSize(mapSize);
            float zoneDistance = Mathf.Min(3000f, size.Bounds.Max.x - 600f);
            List<MapNode> nodes = new List<MapNode>
            {
                new MapNode(MapNodeKind.CapturableZone, Vector3.zero, PlayerId.None, _radii.MainZone)
                    { IsMain = true, Mirror = 0 },
                new MapNode(MapNodeKind.CapturableZone, Vector3.right * zoneDistance, PlayerId.None, _radii.Zone)
                    { Mirror = 2 },
                new MapNode(MapNodeKind.CapturableZone, Vector3.left * zoneDistance, PlayerId.None, _radii.Zone)
                    { Mirror = 1 }
            };
            List<MapLane> lanes = new List<MapLane>
            {
                new MapLane(new[] { Vector3.zero, nodes[1].Center }),
                new MapLane(new[] { Vector3.zero, nodes[2].Center })
            };
            MainLineBlocker blocker = new MainLineBlocker(_settings);
            Assert.That(blocker.TryReserve(size, Array.Empty<MapStation>(), nodes, lanes,
                Array.Empty<HazardShape>(), out List<FieldVolume> reserved), Is.True);
            // A tiny existing natural cluster must not suppress the meaningful fallback field.
            List<AsteroidField> fields = new List<AsteroidField>
            {
                new AsteroidField(new[] { new FieldVolume(Vector3.right * (zoneDistance * 0.65f), 45f) },
                    Array.Empty<AsteroidSpot>(), _settings.FieldFloor, _settings.FieldCeiling)
            };
            blocker.Fill(size, nodes, Array.Empty<HazardShape>(), reserved, fields, new Random(17));
            Assert.That(fields.Count, Is.EqualTo(3));
            foreach (MapNode zone in nodes.Skip(1))
                Assert.That(MainLineBlocker.IsBlocked(nodes[0].Center, zone.Center,
                    _radii.MainZone + _settings.ZoneClearance + size.PocketThickness,
                    _radii.Zone + _settings.ZoneClearance + size.PocketThickness,
                    size.LaneWidth, fields, Array.Empty<HazardShape>()), Is.True,
                    "The fallback wall must provide continuous lane-scale cover across each relay line.");
            AsteroidField first = fields[1];
            AsteroidField mirror = fields[2];
            Assert.That(first.Volumes.Count, Is.GreaterThan(1));
            float width = first.Volumes.Max(volume => volume.Center.z + volume.Radius) -
                first.Volumes.Min(volume => volume.Center.z - volume.Radius);
            Assert.That(width, Is.GreaterThanOrEqualTo(size.LaneWidth));
            FieldVolume[] ordered = first.Volumes.OrderBy(volume => volume.Center.z).ToArray();
            for (int i = 1; i < ordered.Length; i++)
                Assert.That(PlanarGeometry.Distance(ordered[i - 1].Center, ordered[i].Center),
                    Is.LessThan(ordered[i - 1].Radius + ordered[i].Radius), "The wall must form one connected obstacle.");
            Assert.That(first.Rocks.Count, Is.GreaterThan(5), "The field must use area-based rock scatter.");
            foreach (AsteroidSpot rock in first.Rocks)
                Assert.That(first.Volumes.Any(volume =>
                    PlanarGeometry.Distance(rock.Position, volume.Center) <= volume.Radius), Is.True);
            Assert.That(mirror.Volumes.Count, Is.EqualTo(first.Volumes.Count));
            Assert.That(mirror.Rocks.Count, Is.EqualTo(first.Rocks.Count));
            foreach (FieldVolume volume in first.Volumes)
                Assert.That(mirror.Volumes.Any(other =>
                    PlanarGeometry.Distance(other.Center, -volume.Center) < TOLERANCE &&
                    Mathf.Abs(other.Radius - volume.Radius) < TOLERANCE), Is.True);
            foreach (AsteroidField field in fields.Skip(1))
                foreach (FieldVolume volume in field.Volumes)
                    foreach (MapLane lane in lanes)
                        Assert.That(PlanarGeometry.DistanceToPolyline(volume.Center, lane.Points) - volume.Radius,
                            Is.GreaterThanOrEqualTo(size.MinLaneWidth * 0.5f - TOLERANCE));
            Assert.That(lanes.All(lane => lane.Points.Count > 2), Is.True, "Crossing lanes must bend around the field.");
        }

        [Test]
        public void MainRelayBlocker_DistantFieldMassCannotMakeATinyIntersectionCountAsBroadCover()
        {
            MapSizeSettings size = _settings.GetSize(MapSize.Large);
            AsteroidField disconnected = new AsteroidField(new[]
                {
                    new FieldVolume(Vector3.zero, 45f),
                    new FieldVolume(Vector3.forward * 1200f, 600f)
                }, Array.Empty<AsteroidSpot>(), _settings.FieldFloor, _settings.FieldCeiling);
            Assert.That(MainLineBlocker.IsBlocked(Vector3.left * 2000f, Vector3.right * 2000f,
                0f, 0f, size.LaneWidth, new[] { disconnected }, Array.Empty<HazardShape>()), Is.False);
            AsteroidField broad = new AsteroidField(new[] { new FieldVolume(Vector3.zero, 600f) },
                Array.Empty<AsteroidSpot>(), _settings.FieldFloor, _settings.FieldCeiling);
            Assert.That(MainLineBlocker.IsBlocked(Vector3.left * 2000f, Vector3.right * 2000f,
                0f, 0f, size.LaneWidth, new[] { broad }, Array.Empty<HazardShape>()), Is.True);
        }

        [Test]
        public void Hazards_PreferMainToAdditionalRelayLinesWhenThereIsRoom()
        {
            MapGenerationSettings settings = UnityEngine.Object.Instantiate(_settings);
            try
            {
                SerializedObject serialized = new SerializedObject(settings);
                SerializedProperty value = serialized.FindProperty("sizes.keyValue").GetArrayElementAtIndex(2)
                    .FindPropertyRelative("value");
                value.FindPropertyRelative("<HazardCount>k__BackingField").intValue = 2;
                value.FindPropertyRelative("<HazardLengthShare>k__BackingField").vector2Value = new Vector2(0.1f, 0.1f);
                value.FindPropertyRelative("<HazardHalfWidthShare>k__BackingField").vector2Value = new Vector2(0.01f, 0.01f);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                MapNode[] nodes =
                {
                    new MapNode(MapNodeKind.CapturableZone, Vector3.zero, PlayerId.None, _radii.MainZone)
                        { IsMain = true, Mirror = 0 },
                    new MapNode(MapNodeKind.CapturableZone, Vector3.right * 3000f, PlayerId.None, _radii.Zone)
                        { Mirror = 2 },
                    new MapNode(MapNodeKind.CapturableZone, Vector3.left * 3000f, PlayerId.None, _radii.Zone)
                        { Mirror = 1 }
                };
                PlayerSlot[] players = CreateRoster(2, FactionType.Separatist, EnemyAiDifficulty.Medium);
                MapSizeSettings size = settings.GetSize(MapSize.Large);
                MapStation[] stations = new StationPlacer(settings, _radii).Place(size, players, new Random(11));
                HazardPlacer placer = new HazardPlacer(settings, new PointOfInterestPlacer(settings, _radii));
                IReadOnlyList<HazardSpot> spots = placer.Place(size, stations, nodes, 11);
                Assert.That(spots.Count, Is.EqualTo(2));
                foreach (HazardSpot spot in spots)
                {
                    Assert.That(Mathf.Abs(spot.Center.z), Is.LessThan(TOLERANCE));
                    Assert.That(Mathf.Abs(spot.Center.x), Is.InRange(1050f, 1950f));
                    Assert.That(Mathf.Abs(Vector3.Dot(spot.Frame.Along, Vector3.right)), Is.LessThan(TOLERANCE));
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [TestCase(MapSize.Small)]
        [TestCase(MapSize.Medium)]
        [TestCase(MapSize.Large)]
        public void Survival_SatellitePadsBelongToPlayerAndSurroundEachMiningPoint(MapSize mapSize)
        {
            BattleDefinition battle = SurvivalPreset.Create(new SurvivalSettings(FactionType.Empire, null,
                FactionType.Rebellion, mapSize, EnemyAiDifficulty.Medium, true)).Build();
            MapLayout layout = new SurvivalLayoutGenerator(_settings, _radii).Generate(battle);
            int pads = 0;
            int mines = 0;
            foreach (SiteSpot site in layout.Sites)
            {
                AssertInside(site.Center, layout, "Survival capture site");
                Assert.That(site.PointId, Is.InRange(0, _settings.GetSize(mapSize).SurvivalMiningSiteCount - 1));
                if (site.Kind == CaptureSiteKind.Mining)
                {
                    mines++;
                    Assert.That(site.Owner, Is.EqualTo(PlayerId.None));
                    continue;
                }
                pads++;
                Assert.That(site.Kind, Is.EqualTo(CaptureSiteKind.Satellite));
                Assert.That(site.Owner, Is.EqualTo(battle.Human.Slot.Id));
                foreach (SiteSpot other in layout.Sites)
                    if (other.Center != site.Center)
                        Assert.That(PlanarGeometry.Distance(site.Center, other.Center),
                            Is.GreaterThanOrEqualTo(_settings.SatelliteFootprintRadius + GetSiteRadius(other.Kind) - TOLERANCE));
                foreach (ZoneSpot zone in layout.Zones)
                    Assert.That(PlanarGeometry.Distance(site.Center, zone.Center),
                        Is.GreaterThanOrEqualTo(_settings.SatelliteFootprintRadius + _radii.Zone));
            }
            Assert.That(pads, Is.EqualTo(_settings.GetSize(mapSize).SurvivalMiningSiteCount * 3));
            Assert.That(pads, Is.EqualTo(mines));
        }

        private float GetSiteRadius(CaptureSiteKind kind) => kind switch
        {
            CaptureSiteKind.Mining => _radii.MiningSite,
            CaptureSiteKind.Satellite => _settings.SatelliteFootprintRadius,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

        [Test]
        public void Hazards_AreDeterministicAndCanBeDisabled()
        {
            MapGenerationSettings settings = UnityEngine.Object.Instantiate(_settings);
            try
            {
                MapLayoutGenerator generator = new MapLayoutGenerator(settings, _radii);
                MapLayout enabled = generator.Generate(MapSize.Medium, TestPlayers.CreateTeamGame().Players, new Random(7919), 7919);
                MapLayout repeated = generator.Generate(MapSize.Medium, TestPlayers.CreateTeamGame().Players, new Random(7919), 7919);
                Assert.That(Fingerprint(repeated), Is.EqualTo(Fingerprint(enabled)));
                for (int i = 0; i < enabled.Hazards.Count; i++)
                    CollectionAssert.AreEqual(enabled.Hazards[i].Spot.Lobes, repeated.Hazards[i].Spot.Lobes);
                SerializedObject serialized = new SerializedObject(settings);
                serialized.FindProperty("hazards").arraySize = 0;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                MapLayout disabled = generator.Generate(MapSize.Medium, TestPlayers.CreateTeamGame().Players, new Random(7919), 7919);
                Assert.That(disabled.Hazards, Is.Empty);
                Assert.That(enabled.Hazards, Is.Not.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [TestCase(MapSize.Small)]
        [TestCase(MapSize.Medium)]
        [TestCase(MapSize.Large)]
        public void Hazards_AlwaysFitAndAvoidGameplayFeaturesAcrossRosters(MapSize mapSize)
        {
            MapSizeSettings size = _settings.GetSize(mapSize);
            for (int playerCount = 2; playerCount <= 4; playerCount++)
            {
                PlayerSlot[] players = CreateRoster(playerCount, FactionType.Separatist, EnemyAiDifficulty.Medium);
                foreach (int seed in _seeds)
                {
                    string context = $"{mapSize}, {playerCount} players, seed {seed}";
                    MapLayout layout = null;
                    Assert.DoesNotThrow(() => layout = GetLayout(mapSize, players, seed), context);
                    Assert.That(layout.Hazards.Count, Is.EqualTo(size.HazardCount), context);
                    if (size.HazardCount == 2)
                    {
                        Assert.That(layout.Hazards[0].Data.HazardType, Is.Not.EqualTo(layout.Hazards[1].Data.HazardType), context);
                        // Preferred islands fold across the relay line; fallback islands use the team split.
                        Vector3 axis = layout.Hazards[0].Spot.Center.normalized;
                        foreach (HazardShape hazard in layout.Hazards)
                        {
                            Vector3 center = hazard.Spot.Center;
                            Assert.That(Mathf.Abs(axis.x * center.z - axis.z * center.x), Is.LessThan(0.01f), context);
                            Assert.That(hazard.Spot.Frame.IsFolded, Is.True, context);
                            Assert.That(Mathf.Abs(Vector3.Dot(hazard.Spot.Frame.Along, axis)), Is.LessThan(0.01f), context);
                        }
                    }
                    for (int i = 0; i < layout.Hazards.Count; i++)
                    {
                        HazardShape band = layout.Hazards[i];
                        HazardSpot spot = band.Spot;
                        HazardSpot mirror = layout.Hazards[i ^ 1].Spot;
                        Assert.That(mirror.Lobes.Count, Is.EqualTo(spot.Lobes.Count), context);
                        for (int p = 0; p < spot.Lobes.Count; p++)
                        {
                            Assert.That((spot.Lobes[p].Center + mirror.Lobes[p].Center).sqrMagnitude, Is.LessThan(TOLERANCE), context);
                            Assert.That(spot.Lobes[p].Width, Is.EqualTo(mirror.Lobes[p].Width), context);
                            Assert.That(spot.Lobes[p].Length, Is.EqualTo(mirror.Lobes[p].Length), context);
                        }
                        Assert.That(band.Bounds.min.x, Is.GreaterThanOrEqualTo(size.Bounds.Min.x), context);
                        Assert.That(band.Bounds.max.x, Is.LessThanOrEqualTo(size.Bounds.Max.x), context);
                        Assert.That(band.Bounds.min.z, Is.GreaterThanOrEqualTo(size.Bounds.Min.y), context);
                        Assert.That(band.Bounds.max.z, Is.LessThanOrEqualTo(size.Bounds.Max.y), context);
                        for (int j = i + 1; j < layout.Hazards.Count; j++)
                            Assert.That(spot.GetClearance(layout.Hazards[j].Spot), Is.GreaterThan(0f), context);
                        foreach (PlayerSlot player in players)
                            Assert.That(band.Contains(layout.GetStationPosition(player.Id),
                                _settings.GetStationRadius(player.Faction) + _settings.ZoneClearance, false), Is.False, context);
                        foreach (ZoneSpot zone in layout.Zones)
                            Assert.That(band.Contains(zone.Center,
                                (zone.IsMain ? _radii.MainZone : _radii.Zone) + _settings.ZoneClearance, false), Is.False, context);
                        foreach (SiteSpot site in layout.Sites)
                            Assert.That(band.Contains(site.Center, GetSiteRadius(site.Kind), false), Is.False, context);
                        foreach (AsteroidField field in layout.Fields)
                        {
                            foreach (FieldVolume volume in field.Volumes)
                                Assert.That(band.Contains(volume.Center, volume.Radius, false), Is.False, context);
                            foreach (AsteroidSpot rock in field.Rocks)
                                Assert.That(band.Contains(rock.Position, size.FieldCellSize, false), Is.False, context);
                        }
                    }
                }
            }
        }

        [Test]
        public void FieldDensity_ClearsBandAndKeepsLaneThroughItsFringeOpen()
        {
            MapSizeSettings size = _settings.GetSize(MapSize.Small);
            FieldDensity original = new FieldDensity(Array.Empty<MapStation>(), Array.Empty<MapNode>(),
                Array.Empty<MapLane>(), _settings, size, new Random(42), Array.Empty<HazardShape>());
            FieldGrid grid = new FieldGrid(size.Bounds, size.FieldCellSize);
            Vector3 center = Vector3.zero;
            bool found = false;
            for (int i = 0; i < grid.Count; i++)
                if (original.IsField(grid.GetCenter(i)))
                {
                    center = grid.GetCenter(i);
                    found = true;
                    break;
                }
            Assert.That(found, Is.True);
            HazardShape spot = TestHazards.CreateEllipse(_settings.Hazards[0], center, Vector3.right, 500f, 150f, 10f);
            MapLane lane = new MapLane(new[] { center + Vector3.back * 1000f, center + Vector3.forward * 1000f });
            FieldDensity withHazard = new FieldDensity(Array.Empty<MapStation>(), Array.Empty<MapNode>(),
                new[] { lane }, _settings, size, new Random(42), new[] { spot });
            Assert.That(withHazard.IsField(center), Is.False);
            Assert.That(withHazard.IsField(center + Vector3.forward * (150f + size.FieldCellSize * 2f + 30f)), Is.False);
        }

        [OneTimeSetUp]
        public void CreateLayoutCache() => _layouts = new Dictionary<string, MapLayout>();

        [OneTimeTearDown]
        public void ReleaseLayoutCache() => _layouts = null;

        [SetUp]
        public void SetUp()
        {
            _settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(SETTINGS_PATH);
            CaptureSiteData siteData = AssetDatabase.LoadAssetAtPath<CaptureSiteData>(
                "Assets/Settings/Data/Models/CaptureSites/CaptureSiteData.asset");
            CaptureSiteView miningSite = AssetDatabase.LoadAssetAtPath<CaptureSiteView>(MINING_PREFAB_PATH);
            _radii = new MapFeatureRadii(
                AssetDatabase.LoadAssetAtPath<ReinforcementZoneView>(ZONE_PREFAB_PATH).Radius,
                siteData.GetSettings(CaptureSiteKind.Mining).Radius * Mathf.Max(Mathf.Abs(miningSite.transform.lossyScale.x), Mathf.Abs(miningSite.transform.lossyScale.z)),
                AssetDatabase.LoadAssetAtPath<ReinforcementZoneView>(MAIN_ZONE_PREFAB_PATH).Radius);
            _generator = new MapLayoutGenerator(_settings, _radii);
        }

        [TestCase(MapSize.Medium, 1.5f)]
        [TestCase(MapSize.Large, 2.25f)]
        public void MapSide_ScalesFromSmallMap(MapSize mapSize, float expectedScale)
        {
            float smallSide = GetSide(_settings.GetSize(MapSize.Small).Bounds.Max, _settings.GetSize(MapSize.Small).Bounds.Min);
            float side = GetSide(_settings.GetSize(mapSize).Bounds.Max, _settings.GetSize(mapSize).Bounds.Min);

            Assert.That(side / smallSide, Is.EqualTo(expectedScale).Within(TOLERANCE));
        }

        [Test]
        public void Generate_EverySizeAndDifficulty_KeepsFeaturesInsideBounds()
        {
            foreach (MapSize mapSize in Enum.GetValues(typeof(MapSize)))
            {
                foreach (EnemyAiDifficulty difficulty in Enum.GetValues(typeof(EnemyAiDifficulty)))
                {
                    foreach (int seed in _seeds)
                    {
                        MapLayout layout = Generate(mapSize, difficulty, seed);
                        MapSizeSettings size = _settings.GetSize(mapSize);
                        foreach (ZoneSpot zone in layout.Zones)
                        {
                            AssertInside(zone.Center, layout, $"{mapSize} zone");
                        }

                        foreach (SiteSpot site in layout.Sites)
                        {
                            AssertInside(site.Center, layout, $"{mapSize} site");
                        }

                        foreach (AsteroidField field in layout.Fields)
                        {
                            foreach (FieldVolume volume in field.Volumes)
                            {
                                AssertInside(volume.Center, layout, $"{mapSize} field volume");
                            }
                        }

                        Assert.That(layout.Zones.Count, Is.EqualTo(1 + 2 * size.AdditionalZonePairCount + 2));
                        Assert.That(layout.Sites.Count, Is.EqualTo(
                            4 + _settings.MainZoneMiningCount + _settings.MainZoneSatelliteCount +
                            2 * size.AdditionalZonePairCount * (1 + _settings.AdditionalZoneSatelliteCount)));
                    }
                }
            }
        }

        [Test]
        public void Stations_SitInDiagonallyOppositeCorners()
        {
            foreach (MapSize mapSize in Enum.GetValues(typeof(MapSize)))
            {
                MapSizeSettings size = _settings.GetSize(mapSize);
                Vector3 center = PlanarGeometry.GetCenter(size.Bounds);
                HashSet<Vector3> positions = new HashSet<Vector3>();
                foreach (int seed in _seeds)
                {
                    PlayerSlot[] players = CreateRoster(2, FactionType.Rebellion, EnemyAiDifficulty.Medium);
                    MapStation[] stations = new StationPlacer(_settings, _radii).Place(size, players, new Random(seed));
                    Assert.That(PlanarGeometry.Distance(stations[1].Position,
                        PlanarGeometry.Reflect(stations[0].Position, center)), Is.LessThan(TOLERANCE));
                    float insetRadius = Mathf.Max(stations[0].Radius, stations[1].Radius);
                    Vector2 baseOffset = (size.Bounds.Max - size.Bounds.Min) * 0.5f -
                        Vector2.one * (insetRadius + _settings.StationEdgeDistance);
                    foreach (MapStation station in stations)
                    {
                        Vector3 offset = station.Position - center;
                        Assert.That(Mathf.Abs(Mathf.Abs(offset.x) - baseOffset.x),
                            Is.LessThanOrEqualTo(size.StationCornerJitter + TOLERANCE));
                        Assert.That(Mathf.Abs(Mathf.Abs(offset.z) - baseOffset.y),
                            Is.LessThanOrEqualTo(size.StationCornerJitter + TOLERANCE));
                        Assert.That(Mathf.Abs(offset.x), Is.GreaterThan((size.Bounds.Max.x - center.x) * 0.5f));
                        Assert.That(Mathf.Abs(offset.z), Is.GreaterThan((size.Bounds.Max.y - center.z) * 0.5f));
                        float mineReach = (station.Radius + _radii.MiningSite + 15f) *
                            Mathf.Cos(15f * Mathf.Deg2Rad) + _radii.MiningSite;
                        float padReach = (station.Radius + _settings.SatelliteFootprintRadius + 15f) *
                            Mathf.Cos(15f * Mathf.Deg2Rad) + _settings.SatelliteFootprintRadius;
                        float homeReach = Mathf.Max(mineReach, padReach);
                        Assert.That(size.Bounds.Max.x - center.x - Mathf.Abs(offset.x),
                            Is.GreaterThanOrEqualTo(homeReach - TOLERANCE), "Outward jitter must retain behind-site room.");
                        Assert.That(size.Bounds.Max.y - center.z - Mathf.Abs(offset.z),
                            Is.GreaterThanOrEqualTo(homeReach - TOLERANCE), "Outward jitter must retain behind-site room.");
                        positions.Add(new Vector3(Mathf.Abs(offset.x), 0f, Mathf.Abs(offset.z)));
                    }
                }
                Assert.That(positions.Count, Is.GreaterThan(1), "Stations must vary within their corner across seeds.");
            }
        }

        private static TeamId GetNearestTeam(Vector3 position, IReadOnlyList<MapStation> stations,
            IReadOnlyList<PlayerSlot> players)
        {
            MapStation nearest = stations.OrderBy(station => PlanarGeometry.Distance(position, station.Position)).First();
            return players.Single(player => player.Id == nearest.Owner).Team;
        }

        [Test]
        public void Planet_StaysAwayFromBorders()
        {
            foreach (MapSize mapSize in Enum.GetValues(typeof(MapSize)))
            {
                foreach (int seed in _seeds)
                {
                    MapLayout layout = Generate(mapSize, EnemyAiDifficulty.Medium, seed);
                    float inset = GetSide(layout.SizeRange.Max, layout.SizeRange.Min) * _settings.PlanetBorderInset;

                    Assert.That(layout.PlanetPosition.x, Is.InRange(
                        layout.SizeRange.Min.x + inset - TOLERANCE, layout.SizeRange.Max.x - inset + TOLERANCE));
                    Assert.That(layout.PlanetPosition.z, Is.InRange(
                        layout.SizeRange.Min.y + inset - TOLERANCE, layout.SizeRange.Max.y - inset + TOLERANCE));
                }
            }
        }

        [TestCase(MapSize.Small, 7)]
        [TestCase(MapSize.Medium, 7)]
        [TestCase(MapSize.Large, 7)]
        [TestCase(MapSize.Small, 19)]
        [TestCase(MapSize.Medium, 19)]
        [TestCase(MapSize.Large, 19)]
        public void Fields_LeaveLanesPassableAndEveryPointOfInterestConnected(MapSize mapSize, int seed)
        {
            MapLayout layout = Generate(mapSize, EnemyAiDifficulty.Medium, seed);
            MapSizeSettings size = _settings.GetSize(mapSize);
            // Volumes may reach half a raster cell past the rasterised lane edge.
            float narrowestWidth = Mathf.Min(size.MinLaneWidth, size.PocketEntranceWidth);
            float passableHalfWidth = (narrowestWidth - size.FieldCellSize) * 0.5f;

            foreach (AsteroidField field in layout.Fields)
            {
                foreach (FieldVolume volume in field.Volumes)
                {
                    foreach (MapLane lane in layout.Lanes)
                    {
                        Assert.That(PlanarGeometry.DistanceToPolyline(volume.Center, lane.Points) - volume.Radius,
                            Is.GreaterThanOrEqualTo(passableHalfWidth), "An asteroid field blocks a lane.");
                    }
                }
            }

            foreach (ZoneSpot zone in layout.Zones)
            {
                Assert.That(IsLaneEndpoint(layout, zone.Center), Is.True, $"Zone at {zone.Center} has no lane.");
            }

            foreach (SiteSpot site in layout.Sites)
            {
                Assert.That(IsLaneEndpoint(layout, site.Center), Is.True, $"Site at {site.Center} has no lane.");
            }
        }

        [Test]
        public void Fields_StayOutOfStationFootprints()
        {
            PlayerRoster teamGame = TestPlayers.CreateTeamGame();
            foreach (MapSize mapSize in Enum.GetValues(typeof(MapSize)))
            {
                foreach (int seed in _seeds)
                {
                    MapLayout layout = GetLayout(mapSize, teamGame.Players, seed);
                    foreach (PlayerSlot player in teamGame.Players)
                    {
                        Vector3 station = layout.GetStationPosition(player.Id);
                        float footprint = _settings.GetStationRadius(player.Faction) + _settings.ZoneClearance;
                        foreach (AsteroidField field in layout.Fields)
                        {
                            foreach (FieldVolume volume in field.Volumes)
                            {
                                Assert.That(PlanarGeometry.Distance(volume.Center, station) - volume.Radius,
                                    Is.GreaterThanOrEqualTo(footprint), $"{mapSize} seed {seed} volume");
                            }

                            foreach (AsteroidSpot rock in field.Rocks)
                            {
                                Assert.That(PlanarGeometry.Distance(rock.Position, station),
                                    Is.GreaterThanOrEqualTo(footprint), $"{mapSize} seed {seed} rock");
                            }
                        }
                    }
                }
            }
        }

        [Test]
        public void Rocks_FollowTheLayerShares()
        {
            int[] counts = new int[Enum.GetValues(typeof(AsteroidSize)).Length];
            int total = 0;
            foreach (AsteroidField field in Generate(MapSize.Large, EnemyAiDifficulty.Medium, 5).Fields)
            {
                foreach (AsteroidSpot rock in field.Rocks)
                {
                    counts[(int)rock.Size]++;
                    total++;
                }
            }

            Assert.That(total, Is.GreaterThan(0));
            Assert.That((float)counts[(int)AsteroidSize.Debris] / total, Is.GreaterThan(0.4f));
            Assert.That((float)counts[(int)AsteroidSize.Large] / total, Is.LessThan(0.3f));
        }

        [Test]
        public void TeamGame_EveryMapSize_GivesEachPlayerACornerWithTeammatesSharingAnEdge()
        {
            PlayerRoster teamGame = TestPlayers.CreateTeamGame();
            foreach (MapSize mapSize in new[] { MapSize.Small, MapSize.Medium, MapSize.Large })
            {
                foreach (int seed in _seeds)
                {
                    MapLayout layout = GetLayout(mapSize, teamGame.Players, seed);
                    Vector3 human = layout.GetStationPosition(TestPlayers.Human);
                    Vector3 ally = layout.GetStationPosition(TestPlayers.Ally);
                    Vector3 enemy = layout.GetStationPosition(TestPlayers.Enemy);
                    Vector3 secondEnemy = layout.GetStationPosition(TestPlayers.SecondEnemy);

                    // Neighbouring corners share one coordinate sign; the enemy team holds the opposite edge.
                    Assert.That(SharesEdge(human, ally), Is.True, $"{mapSize} seed {seed} allies");
                    Assert.That(SharesEdge(enemy, secondEnemy), Is.True, $"{mapSize} seed {seed} enemies");
                    Assert.That(human, Is.Not.EqualTo(enemy));
                    Assert.That(human, Is.Not.EqualTo(secondEnemy));
                    Assert.That(layout.Zones.Count,
                        Is.EqualTo(1 + 2 * _settings.GetSize(mapSize).AdditionalZonePairCount + teamGame.Players.Count));
                }
            }
        }

        [Test]
        public void ThreePlayers_GeneratesOnEveryMapSize()
        {
            PlayerRoster teamGame = TestPlayers.CreateTeamGame();
            PlayerSlot[] threePlayers = { teamGame.Players[0], teamGame.Players[1], teamGame.Players[2] };
            foreach (MapSize mapSize in new[] { MapSize.Small, MapSize.Medium, MapSize.Large })
            {
                foreach (int seed in _seeds)
                {
                    Assert.DoesNotThrow(() => GetLayout(mapSize, threePlayers, seed),
                        $"{mapSize} seed {seed}");
                }
            }
        }

        // Republic, Rebellion and Empire stations share the largest footprint, so all-Republic rosters are the tightest fit.
        [TestCase(MapSize.Small)]
        [TestCase(MapSize.Medium)]
        [TestCase(MapSize.Large)]
        [Timeout(600000)]
        public void Generate_EveryRosterAndDifficulty_FindsRoom(MapSize mapSize)
        {
            for (int playerCount = 2; playerCount <= 4; playerCount++)
            {
                foreach (FactionType opponentFaction in new[] { FactionType.Republic, FactionType.Separatist })
                {
                    foreach (EnemyAiDifficulty difficulty in Enum.GetValues(typeof(EnemyAiDifficulty)))
                    {
                        PlayerSlot[] players = CreateRoster(playerCount, opponentFaction, difficulty);
                        foreach (int seed in _rosterSeeds)
                        {
                            Assert.DoesNotThrow(() => GetLayout(mapSize, players, seed),
                                $"{mapSize} {playerCount} players vs {opponentFaction} {difficulty} seed {seed}");
                        }
                    }
                }
            }
        }

        private static PlayerSlot[] CreateRoster(int playerCount, FactionType opponentFaction, EnemyAiDifficulty difficulty)
        {
            PlayerSlot[] players = new PlayerSlot[playerCount];
            for (int i = 0; i < playerCount; i++)
            {
                bool isHumanTeam = i % 2 == 0;
                players[i] = new PlayerSlot(new PlayerId(i), new TeamId(i % 2),
                    isHumanTeam ? FactionType.Republic : opponentFaction,
                    i == 0 ? PlayerController.Human : PlayerController.Ai, difficulty, i);
            }

            return players;
        }

        private static bool SharesEdge(Vector3 first, Vector3 second)
        {
            return Mathf.Approximately(first.x, second.x) != Mathf.Approximately(first.z, second.z);
        }

        [Test]
        public void SameSeed_ProducesSameLayout()
        {
            PlayerRoster duel = TestPlayers.CreateDuel(EnemyAiDifficulty.Hard);
            MapLayout first = _generator.Generate(MapSize.Medium, duel.Players, new Random(42), 42);
            MapLayout second = _generator.Generate(MapSize.Medium, duel.Players, new Random(42), 42);

            Assert.That(second.GetStationPosition(TestPlayers.Human),
                Is.EqualTo(first.GetStationPosition(TestPlayers.Human)));
            Assert.That(second.Fields.Count, Is.EqualTo(first.Fields.Count));
            Assert.That(second.PlanetPosition, Is.EqualTo(first.PlanetPosition));
        }

        [Test]
        public void AiDifficulty_DoesNotChangeTheLayout()
        {
            // The layout cache keys on generator inputs only; if difficulty ever feeds generation, add it to the key.
            foreach (MapSize mapSize in Enum.GetValues(typeof(MapSize)))
            {
                string expected = Fingerprint(_generator.Generate(mapSize,
                    CreateRoster(4, FactionType.Separatist, EnemyAiDifficulty.Easy), new Random(11), 11));
                foreach (EnemyAiDifficulty difficulty in Enum.GetValues(typeof(EnemyAiDifficulty)))
                    Assert.That(Fingerprint(_generator.Generate(mapSize,
                        CreateRoster(4, FactionType.Separatist, difficulty), new Random(11), 11)),
                        Is.EqualTo(expected), $"{mapSize} {difficulty}");
            }
        }

        [Test]
        public void SavedSkirmishSeed_RebuildsIdenticalLayout()
        {
            BattleDefinition battle = SkirmishPreset.Create(TestPlayers.CreateTeamGame().Players,
                EmpireAtWar.Entities.Planet.PlanetType.Coruscant, MapSize.Large,
                BattleVictoryCondition.DestroyEnemyFleet, 1000f, true, SkirmishPreset.NewMapSeed()).Build();
            BattleDefinition loaded = JsonUtility.FromJson<BattleSetupSave>(
                JsonUtility.ToJson(BattleSetupSave.From(battle))).Rebuild();

            Assert.That(Fingerprint(GenerateFor(loaded)), Is.EqualTo(Fingerprint(GenerateFor(battle))));
        }

        private MapLayout GenerateFor(BattleDefinition battle)
        {
            return _generator.Generate(battle.Map.Size, battle.Slots, new Random(battle.Map.Seed), battle.Map.Seed);
        }

        private static string Fingerprint(MapLayout layout)
        {
            StringBuilder text = new StringBuilder();
            text.Append(layout.PlanetPosition.ToString("R")).Append('|');
            for (int i = 0; i < TestPlayers.CreateTeamGame().Players.Count; i++)
                text.Append(layout.GetStationPosition(new PlayerId(i)).ToString("R")).Append(';');
            foreach (ZoneSpot zone in layout.Zones)
                text.Append(zone.Center.ToString("R")).Append(zone.Owner).Append(zone.IsCapturable).Append(';');
            foreach (SiteSpot site in layout.Sites)
                text.Append(site.Center.ToString("R")).Append(site.Kind).Append(site.Owner).Append(site.PointId).Append(';');
            foreach (MapLane lane in layout.Lanes)
                foreach (Vector3 point in lane.Points)
                    text.Append(point.ToString("R")).Append(';');
            foreach (AsteroidField field in layout.Fields)
            {
                text.Append(field.Floor.ToString("R")).Append(field.Ceiling.ToString("R")).Append('|');
                foreach (FieldVolume volume in field.Volumes)
                    text.Append(volume.Center.ToString("R")).Append(volume.Radius.ToString("R")).Append(';');
                foreach (AsteroidSpot rock in field.Rocks)
                    text.Append(rock.Position.ToString("R")).Append(rock.Rotation.ToString("R"))
                        .Append(rock.Scale.ToString("R")).Append(rock.Size).Append(';');
            }

            return text.ToString();
        }

        private MapLayout Generate(MapSize mapSize, EnemyAiDifficulty difficulty, int seed)
        {
            return GetLayout(mapSize, TestPlayers.CreateDuel(difficulty).Players, seed);
        }

        private MapLayout GetLayout(MapSize mapSize, IReadOnlyList<PlayerSlot> players, int seed)
        {
            // Station placement reads only id, team and faction; difficulty and controller never shape the map.
            StringBuilder key = new StringBuilder().Append(mapSize).Append('|').Append(seed);
            foreach (PlayerSlot player in players)
                key.Append('|').Append(player.Id.Index).Append(':').Append(player.Team.Value).Append(':').Append(player.Faction);
            string id = key.ToString();
            if (!_layouts.TryGetValue(id, out MapLayout layout))
            {
                layout = _generator.Generate(mapSize, players, new Random(seed), seed);
                _layouts.Add(id, layout);
            }
            return layout;
        }

        private static bool IsLaneEndpoint(MapLayout layout, Vector3 position)
        {
            foreach (MapLane lane in layout.Lanes)
            {
                if (PlanarGeometry.Distance(lane.Points[0], position) < TOLERANCE ||
                    PlanarGeometry.Distance(lane.Points[lane.Points.Count - 1], position) < TOLERANCE)
                {
                    return true;
                }
            }

            return false;
        }

        private static void AssertInside(Vector3 position, MapLayout layout, string label)
        {
            Assert.That(position.x, Is.InRange(layout.SizeRange.Min.x, layout.SizeRange.Max.x), label);
            Assert.That(position.z, Is.InRange(layout.SizeRange.Min.y, layout.SizeRange.Max.y), label);
        }

        private static float GetSide(Vector2 max, Vector2 min)
        {
            return max.x - min.x;
        }
    }
}
