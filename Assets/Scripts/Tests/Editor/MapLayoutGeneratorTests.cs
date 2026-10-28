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
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Views.ReinforcementZones;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class MapLayoutGeneratorTests
    {
        private const string SETTINGS_PATH = "Assets/Settings/Data/Models/Map/MapGenerationSettings.asset";
        private const string ZONE_PREFAB_PATH = "Assets/Prefabs/View/ReinforcementZones/ReinforcementZone.prefab";
        private const string MINING_PREFAB_PATH = "Assets/Prefabs/View/CaptureSites/CaptureSite.prefab";
        private const string BATTLE_PREFAB_PATH = "Assets/Prefabs/View/CaptureSites/BattleAsteroidCaptureSite.prefab";

        private const int SEED_COUNT = 20;
        private const int ROSTER_SEED_COUNT = 10;

        private const float TOLERANCE = 0.01f;

        // Neighbouring sites of one mining point are spaced from a 15-unit gap; anything wider belongs to another point.
        private const float MINING_SITE_GAP = 15f;

        private MapGenerationSettings _settings;
        private MapFeatureRadii _radii;
        private MapLayoutGenerator _generator;

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
                    for (int seed = 0; seed < SEED_COUNT; seed++)
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
                            size.MiningPointCount * PointOfInterestPlacer.SITES_PER_MINING_POINT + size.BattleSiteCount));
                    }
                }
            }
        }

        [Test]
        public void Stations_SitInDiagonallyOppositeCorners()
        {
            for (int seed = 0; seed < SEED_COUNT; seed++)
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
                if (site.FacilityType == SiteFacilityType.BattleAsteroid)
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
                for (int seed = 0; seed < SEED_COUNT; seed++)
                {
                    PlayerRoster duel = TestPlayers.CreateDuel(EnemyAiDifficulty.Medium);
                    MapLayout layout = _generator.Generate(mapSize, duel.Players, new Random(seed));
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
                        float gapLimit = _settings.ZoneClearance + MINING_SITE_GAP + 1.5f * siteSpread + TOLERANCE;
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
                for (int seed = 0; seed < SEED_COUNT; seed++)
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
                for (int seed = 0; seed < SEED_COUNT; seed++)
                {
                    MapLayout layout = Generate(mapSize, EnemyAiDifficulty.Medium, seed);
                    Vector3 center = PlanarGeometry.GetCenter(layout.SizeRange);
                    int central = 0;
                    foreach (SiteSpot site in layout.Sites)
                    {
                        if (site.FacilityType == SiteFacilityType.Mining &&
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
                for (int seed = 0; seed < SEED_COUNT; seed++)
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
                for (int seed = 0; seed < SEED_COUNT; seed++)
                {
                    MapLayout layout = _generator.Generate(mapSize, teamGame.Players, new Random(seed));
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
                for (int seed = 0; seed < SEED_COUNT; seed++)
                {
                    MapLayout layout = _generator.Generate(mapSize, teamGame.Players, new Random(seed));
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
                for (int seed = 0; seed < SEED_COUNT; seed++)
                {
                    Assert.DoesNotThrow(() => _generator.Generate(mapSize, threePlayers, new Random(seed)),
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
                        for (int seed = 0; seed < ROSTER_SEED_COUNT; seed++)
                        {
                            Assert.DoesNotThrow(() => _generator.Generate(mapSize, players, new Random(seed)),
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
            return _generator.Generate(battle.Map.Size, battle.Slots, new Random(battle.Map.Seed));
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
                text.Append(site.Center.ToString("R")).Append(site.FacilityType).Append(';');
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
            return _generator.Generate(mapSize, TestPlayers.CreateDuel(difficulty).Players, new Random(seed));
        }

        private static List<Vector3> GetMiningSites(MapLayout layout)
        {
            List<Vector3> mines = new List<Vector3>();
            foreach (SiteSpot site in layout.Sites)
            {
                if (site.FacilityType == SiteFacilityType.Mining)
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
