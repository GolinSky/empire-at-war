using System;
using System.Collections.Generic;
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
        private const string MINING_PREFAB_PATH = "Assets/Prefabs/View/CaptureSites/CaptureSite.prefab";
        private const string BATTLE_PREFAB_PATH = "Assets/Prefabs/View/CaptureSites/BattleAsteroidCaptureSite.prefab";

        private const int SEED_COUNT = 20;
        private const int ROSTER_SEED_COUNT = 10;
        // Includes seeds that once broke lanes.
        private static readonly int[] QUICK_SEEDS = { 0, 7, 19 };

        private const float TOLERANCE = 0.01f;

        // Neighbouring sites of one mining point are spaced from a 15-unit gap; anything wider belongs to another point.
        private const float MINING_SITE_GAP = 15f;

        private MapGenerationSettings _settings;
        private MapFeatureRadii _radii;
        private MapLayoutGenerator _generator;
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
        public void Satellites_FormClearDualTrianglesAcrossSizesRostersDifficultiesAndSeeds()
        {
            foreach (MapSize mapSize in Enum.GetValues(typeof(MapSize)))
            foreach (EnemyAiDifficulty difficulty in Enum.GetValues(typeof(EnemyAiDifficulty)))
            for (int playerCount = 2; playerCount <= 4; playerCount++)
            foreach (int seed in _seeds)
            {
                MapSizeSettings size = _settings.GetSize(mapSize);
                Random random = new Random(seed);
                PlayerSlot[] players = CreateRoster(playerCount, FactionType.Separatist, difficulty);
                MapStation[] stations = new StationPlacer(_settings).Place(size.Bounds, players, random);
                PointOfInterestPlacer placer = new PointOfInterestPlacer(_settings, _radii);
                List<MapNode> nodes = new List<MapNode>();
                bool placed = false;
                for (int attempt = 0; attempt < 60 && !placed; attempt++)
                    placed = placer.TryPlace(size, stations, random, nodes);
                string context = $"{mapSize}, {difficulty}, {playerCount} players, seed {seed}";
                Assert.That(placed, Is.True, context);
                int mines = 0;
                int pads = 0;
                foreach (MapNode pad in nodes)
                {
                    if (pad.Kind == MapNodeKind.MiningSite) mines++;
                    if (pad.Kind != MapNodeKind.SatelliteSite) continue;
                    pads++;
                    Assert.That(pad.Owner, Is.EqualTo(PlayerId.None), context);
                    Vector3 point = Vector3.zero;
                    int pointMines = 0;
                    int pointPads = 0;
                    foreach (MapNode other in nodes)
                    {
                        if (other.PointId == pad.PointId && other.Kind == MapNodeKind.MiningSite)
                        {
                            point += other.Center;
                            pointMines++;
                        }
                        if (other.PointId == pad.PointId && other.Kind == MapNodeKind.SatelliteSite) pointPads++;
                        if (!ReferenceEquals(other, pad))
                            Assert.That(PlanarGeometry.Distance(pad.Center, other.Center) + TOLERANCE,
                                Is.GreaterThanOrEqualTo(pad.Radius + other.Radius), context);
                    }
                    Assert.That(pointMines, Is.EqualTo(3), context);
                    Assert.That(pointPads, Is.EqualTo(3), context);
                    point /= 3f;
                    float mineSpread = (2f * _radii.MiningSite + MINING_SITE_GAP) *
                        PointOfInterestPlacer.MINING_SITE_SPACING_SHARE / Mathf.Sqrt(3f);
                    Assert.That(PlanarGeometry.Distance(pad.Center, point),
                        Is.EqualTo(mineSpread + _settings.SatellitePadOffset).Within(TOLERANCE), context);
                    foreach (MapStation station in stations)
                        Assert.That(PlanarGeometry.Distance(pad.Center, station.Position) + TOLERANCE,
                            Is.GreaterThanOrEqualTo(pad.Radius + station.Radius + _settings.ZoneClearance), context);
                    if (nodes[pad.Mirror] != pad)
                        Assert.That(PlanarGeometry.Distance(nodes[pad.Mirror].Center,
                            PlanarGeometry.Reflect(pad.Center, PlanarGeometry.GetCenter(size.Bounds))),
                            Is.LessThan(TOLERANCE), context);
                }
                Assert.That(pads, Is.EqualTo(mines), context);
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
                Assert.That(site.PointId, Is.InRange(0, _settings.GetSize(mapSize).MiningPointCount - 1));
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
            Assert.That(pads, Is.EqualTo(_settings.GetSize(mapSize).MiningPointCount * 3));
            Assert.That(pads, Is.EqualTo(mines));
        }

        private float GetSiteRadius(CaptureSiteKind kind) => kind switch
        {
            CaptureSiteKind.Mining => _radii.MiningSite,
            CaptureSiteKind.Battle => _radii.BattleSite,
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
                    Assert.DoesNotThrow(() => layout = _generator.Generate(mapSize, players, new Random(seed), seed), context);
                    Assert.That(layout.Hazards.Count, Is.EqualTo(size.HazardCount), context);
                    if (size.HazardCount == 2)
                    {
                        Assert.That(layout.Hazards[0].Data.HazardType, Is.Not.EqualTo(layout.Hazards[1].Data.HazardType), context);
                        // Each island sits on the axis equidistant to the teams and is its own mirror image across it.
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
                            Assert.That(band.Contains(zone.Center, _radii.Zone + _settings.ZoneClearance, false), Is.False, context);
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

        [SetUp]
        public void SetUp()
        {
            _settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(SETTINGS_PATH);
            _radii = new MapFeatureRadii(
                AssetDatabase.LoadAssetAtPath<ReinforcementZoneView>(ZONE_PREFAB_PATH).Radius,
                AssetDatabase.LoadAssetAtPath<CaptureSiteView>(MINING_PREFAB_PATH).Radius,
                AssetDatabase.LoadAssetAtPath<CaptureSiteView>(BATTLE_PREFAB_PATH).Radius);
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

                        Assert.That(layout.Zones.Count, Is.EqualTo(size.CapturableZoneCount + 2));
                        Assert.That(layout.Sites.Count, Is.EqualTo(
                            size.MiningPointCount * PointOfInterestPlacer.SITES_PER_MINING_POINT * 2 + size.BattleSiteCount));
                    }
                }
            }
        }

        [Test]
        public void Stations_SitInDiagonallyOppositeCorners()
        {
            foreach (int seed in _seeds)
            {
                MapLayout layout = Generate(MapSize.Medium, EnemyAiDifficulty.Medium, seed);
                Vector3 republic = layout.GetStationPosition(TestPlayers.Human);
                Vector3 separatist = layout.GetStationPosition(TestPlayers.Enemy);

                // Each station is inset by its own footprint, so mixed factions mirror only by corner.
                Assert.That(Mathf.Sign(separatist.x), Is.EqualTo(-Mathf.Sign(republic.x)));
                Assert.That(Mathf.Sign(separatist.z), Is.EqualTo(-Mathf.Sign(republic.z)));
                Assert.That(Mathf.Abs(republic.x), Is.EqualTo(layout.SizeRange.Max.x -
                    _settings.GetStationRadius(FactionType.Republic) - _settings.StationEdgeDistance).Within(TOLERANCE));
                Assert.That(Mathf.Abs(separatist.x), Is.EqualTo(layout.SizeRange.Max.x -
                    _settings.GetStationRadius(FactionType.Separatist) - _settings.StationEdgeDistance).Within(TOLERANCE));
                Assert.That(Mathf.Abs(republic.x), Is.GreaterThan(layout.SizeRange.Max.x * 0.5f));
                Assert.That(Mathf.Abs(republic.z), Is.GreaterThan(layout.SizeRange.Max.y * 0.5f));
            }
        }

        [Test]
        public void CapturableZonesAndBattleSites_ArePointSymmetric()
        {
            MapLayout layout = Generate(MapSize.Large, EnemyAiDifficulty.Hard, 3);

            foreach (ZoneSpot zone in layout.Zones)
            {
                if (zone.IsCapturable)
                {
                    Assert.That(HasZoneAt(layout, -zone.Center), Is.True, $"Zone at {zone.Center} has no mirror.");
                }
            }

            foreach (SiteSpot site in layout.Sites)
            {
                if (site.Kind == CaptureSiteKind.Battle)
                {
                    Assert.That(HasSiteAt(layout, -site.Center), Is.True, $"Site at {site.Center} has no mirror.");
                }
            }
        }

        [Test]
        public void HomeMining_ThreeSitesHugTheBackOfEachStation()
        {
            foreach (MapSize mapSize in Enum.GetValues(typeof(MapSize)))
            {
                foreach (int seed in _seeds)
                {
                    PlayerRoster duel = TestPlayers.CreateDuel(EnemyAiDifficulty.Medium);
                    MapLayout layout = _generator.Generate(mapSize, duel.Players, new Random(seed), seed);
                    Vector3 center = PlanarGeometry.GetCenter(layout.SizeRange);
                    foreach (PlayerSlot player in duel.Players)
                    {
                        Vector3 station = layout.GetStationPosition(player.Id);
                        station.y = 0f;
                        List<Vector3> mines = GetMiningSites(layout);
                        mines.Sort((first, second) =>
                            PlanarGeometry.Distance(station, first).CompareTo(PlanarGeometry.Distance(station, second)));
                        // The two sites facing the station hug it; the third sits one triangle height behind them.
                        float siteSpread = (2f * _radii.MiningSite + MINING_SITE_GAP) *
                            PointOfInterestPlacer.MINING_SITE_SPACING_SHARE / Mathf.Sqrt(3f);
                        float centerDistance = Mathf.Max(_radii.MiningSite + _settings.ZoneClearance +
                            MINING_SITE_GAP + siteSpread * 0.5f, _settings.SatelliteFootprintRadius + _settings.ZoneClearance +
                            siteSpread + _settings.SatellitePadOffset + MINING_SITE_GAP);
                        float gapLimit = centerDistance + siteSpread - _radii.MiningSite + TOLERANCE;
                        for (int i = 0; i < PointOfInterestPlacer.SITES_PER_MINING_POINT; i++)
                        {
                            string label = $"{mapSize} seed {seed} {player.Id} home mine {i}";
                            float gap = PlanarGeometry.Distance(station, mines[i]) -
                                _settings.GetStationRadius(player.Faction) - _radii.MiningSite;
                            Assert.That(gap, Is.LessThanOrEqualTo(gapLimit), label);
                            // Behind the station: on the far side from the map center, toward the corner.
                            Assert.That(Vector3.Dot(mines[i] - station, center - station), Is.LessThan(0f), label);
                        }
                    }
                }
            }
        }

        [Test]
        public void MiningSites_ComeInClustersOfThree()
        {
            float neighbourDistance = (2f * _radii.MiningSite + MINING_SITE_GAP) *
                PointOfInterestPlacer.MINING_SITE_SPACING_SHARE + TOLERANCE;
            foreach (MapSize mapSize in Enum.GetValues(typeof(MapSize)))
            {
                foreach (int seed in _seeds)
                {
                    List<Vector3> mines = GetMiningSites(Generate(mapSize, EnemyAiDifficulty.Medium, seed));
                    // Sites chained by neighbour gaps form one point: a triangle, or an arc behind a station.
                    foreach (Vector3 mine in mines)
                    {
                        List<Vector3> cluster = new List<Vector3> { mine };
                        for (int i = 0; i < cluster.Count; i++)
                        {
                            foreach (Vector3 other in mines)
                            {
                                if (!cluster.Contains(other) && PlanarGeometry.Distance(cluster[i], other) <= neighbourDistance)
                                {
                                    cluster.Add(other);
                                }
                            }
                        }

                        Assert.That(cluster.Count, Is.EqualTo(PointOfInterestPlacer.SITES_PER_MINING_POINT),
                            $"{mapSize} seed {seed} mine at {mine}");
                    }
                }
            }
        }

        [Test]
        public void CentralMining_EverySizePlacesItsPairsNearTheCenter()
        {
            foreach (MapSize mapSize in Enum.GetValues(typeof(MapSize)))
            {
                MapSizeSettings size = _settings.GetSize(mapSize);
                // Point centers stay inside the region; their sites reach out by the site triangle's circumradius.
                float siteSpread = (2f * _radii.MiningSite + MINING_SITE_GAP) *
                    PointOfInterestPlacer.MINING_SITE_SPACING_SHARE / Mathf.Sqrt(3f);
                Vector2 regionHalfSize = (size.Bounds.Max - size.Bounds.Min) * _settings.CentralRegionShare +
                    Vector2.one * siteSpread;
                Assert.That(size.CentralMiningPairCount, Is.GreaterThan(0), $"{mapSize}");
                Assert.That(size.CentralMiningPairCount,
                    Is.LessThanOrEqualTo((size.MiningPointCount - 2) / 2), $"{mapSize}");
                foreach (int seed in _seeds)
                {
                    MapLayout layout = Generate(mapSize, EnemyAiDifficulty.Medium, seed);
                    Vector3 center = PlanarGeometry.GetCenter(layout.SizeRange);
                    int central = 0;
                    foreach (SiteSpot site in layout.Sites)
                    {
                        if (site.Kind == CaptureSiteKind.Mining &&
                            Mathf.Abs(site.Center.x - center.x) <= regionHalfSize.x + TOLERANCE &&
                            Mathf.Abs(site.Center.z - center.z) <= regionHalfSize.y + TOLERANCE)
                        {
                            central++;
                        }
                    }

                    Assert.That(central, Is.GreaterThanOrEqualTo(
                        size.CentralMiningPairCount * 2 * PointOfInterestPlacer.SITES_PER_MINING_POINT), $"{mapSize} seed {seed}");
                }
            }
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
                    MapLayout layout = _generator.Generate(mapSize, teamGame.Players, new Random(seed), seed);
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
                    MapLayout layout = _generator.Generate(mapSize, teamGame.Players, new Random(seed), seed);
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
                        Is.EqualTo(_settings.GetSize(mapSize).CapturableZoneCount + teamGame.Players.Count));
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
                    Assert.DoesNotThrow(() => _generator.Generate(mapSize, threePlayers, new Random(seed), seed),
                        $"{mapSize} seed {seed}");
                }
            }
        }

        // Republic, Rebellion and Empire stations share the largest footprint, so all-Republic rosters are the tightest fit.
        [TestCase(MapSize.Small)]
        [TestCase(MapSize.Medium)]
        [TestCase(MapSize.Large)]
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
                            Assert.DoesNotThrow(() => _generator.Generate(mapSize, players, new Random(seed), seed),
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
            MapLayout first = Generate(MapSize.Medium, EnemyAiDifficulty.Hard, 42);
            MapLayout second = Generate(MapSize.Medium, EnemyAiDifficulty.Hard, 42);

            Assert.That(second.GetStationPosition(TestPlayers.Human),
                Is.EqualTo(first.GetStationPosition(TestPlayers.Human)));
            Assert.That(second.Fields.Count, Is.EqualTo(first.Fields.Count));
            Assert.That(second.PlanetPosition, Is.EqualTo(first.PlanetPosition));
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
            return _generator.Generate(mapSize, TestPlayers.CreateDuel(difficulty).Players, new Random(seed), seed);
        }

        private static List<Vector3> GetMiningSites(MapLayout layout)
        {
            List<Vector3> mines = new List<Vector3>();
            foreach (SiteSpot site in layout.Sites)
            {
                if (site.Kind == CaptureSiteKind.Mining)
                {
                    mines.Add(site.Center);
                }
            }

            return mines;
        }

        private static bool HasZoneAt(MapLayout layout, Vector3 position)
        {
            foreach (ZoneSpot zone in layout.Zones)
            {
                if (PlanarGeometry.Distance(zone.Center, position) < TOLERANCE)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasSiteAt(MapLayout layout, Vector3 position)
        {
            foreach (SiteSpot site in layout.Sites)
            {
                if (PlanarGeometry.Distance(site.Center, position) < TOLERANCE)
                {
                    return true;
                }
            }

            return false;
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
