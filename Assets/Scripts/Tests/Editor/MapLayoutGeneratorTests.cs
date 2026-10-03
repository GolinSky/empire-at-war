using System;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.EnemyFaction.Models;
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

        private const float TOLERANCE = 0.01f;

        private MapGenerationSettings _settings;
        private MapLayoutGenerator _generator;

        [SetUp]
        public void SetUp()
        {
            _settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(SETTINGS_PATH);
            MapFeatureRadii radii = new MapFeatureRadii(
                AssetDatabase.LoadAssetAtPath<ReinforcementZoneView>(ZONE_PREFAB_PATH).Radius,
                AssetDatabase.LoadAssetAtPath<CaptureSiteView>(MINING_PREFAB_PATH).Radius,
                AssetDatabase.LoadAssetAtPath<CaptureSiteView>(BATTLE_PREFAB_PATH).Radius);
            _generator = new MapLayoutGenerator(_settings, radii);
        }

        [TestCase(MapSize.Medium, 1.5f)]
        [TestCase(MapSize.Large, 5f)]
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
                        Assert.That(layout.Sites.Count, Is.EqualTo(size.MiningSiteCount + size.BattleSiteCount));
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

        [TestCase(MapSize.Small)]
        [TestCase(MapSize.Medium)]
        [TestCase(MapSize.Large)]
        public void HomeMining_IsFartherOnHarderDifficulty(MapSize mapSize)
        {
            float easy = 0f;
            float ultraHard = 0f;
            for (int seed = 0; seed < SEED_COUNT; seed++)
            {
                easy += GetClosestMiningDistance(Generate(mapSize, EnemyAiDifficulty.Easy, seed));
                ultraHard += GetClosestMiningDistance(Generate(mapSize, EnemyAiDifficulty.UltraHard, seed));
            }

            Assert.That(ultraHard, Is.GreaterThan(easy));
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

        [TestCase(MapSize.Small)]
        [TestCase(MapSize.Medium)]
        [TestCase(MapSize.Large)]
        public void Fields_LeaveLanesPassableAndEveryPointOfInterestConnected(MapSize mapSize)
        {
            MapLayout layout = Generate(mapSize, EnemyAiDifficulty.Medium, 7);
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
                        Assert.That(MapGeometry.DistanceToPolyline(volume.Center, lane.Points) - volume.Radius,
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
                                Assert.That(MapGeometry.Distance(volume.Center, station) - volume.Radius,
                                    Is.GreaterThanOrEqualTo(footprint), $"{mapSize} seed {seed} volume");
                            }

                            foreach (AsteroidSpot rock in field.Rocks)
                            {
                                Assert.That(MapGeometry.Distance(rock.Position, station),
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

        private MapLayout Generate(MapSize mapSize, EnemyAiDifficulty difficulty, int seed)
        {
            return _generator.Generate(mapSize, TestPlayers.CreateDuel(difficulty).Players, new Random(seed));
        }

        private static float GetClosestMiningDistance(MapLayout layout)
        {
            Vector3 station = layout.GetStationPosition(TestPlayers.Human);
            float closest = float.MaxValue;
            foreach (SiteSpot site in layout.Sites)
            {
                if (site.FacilityType == SiteFacilityType.Mining)
                {
                    closest = Mathf.Min(closest, MapGeometry.Distance(station, site.Center));
                }
            }

            return closest;
        }

        private static bool HasZoneAt(MapLayout layout, Vector3 position)
        {
            foreach (ZoneSpot zone in layout.Zones)
            {
                if (MapGeometry.Distance(zone.Center, position) < TOLERANCE)
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
                if (MapGeometry.Distance(site.Center, position) < TOLERANCE)
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
                if (MapGeometry.Distance(lane.Points[0], position) < TOLERANCE ||
                    MapGeometry.Distance(lane.Points[lane.Points.Count - 1], position) < TOLERANCE)
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
