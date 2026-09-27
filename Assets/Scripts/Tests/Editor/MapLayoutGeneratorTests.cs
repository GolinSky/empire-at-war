using System;
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

                        foreach (ObstacleSpot obstacle in layout.Obstacles)
                        {
                            AssertInside(obstacle.Position, layout, $"{mapSize} obstacle");
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
                Vector3 republic = layout.GetStationPosition(FactionType.Republic);
                Vector3 separatist = layout.GetStationPosition(FactionType.Separatist);

                Assert.That(separatist.x, Is.EqualTo(-republic.x).Within(TOLERANCE));
                Assert.That(separatist.z, Is.EqualTo(-republic.z).Within(TOLERANCE));
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

        [Test]
        public void Walls_LeaveRoadsOpenAndEveryPointOfInterestConnected()
        {
            MapSize mapSize = MapSize.Medium;
            MapLayout layout = Generate(mapSize, EnemyAiDifficulty.Medium, 7);
            float roadHalfWidth = _settings.GetSize(mapSize).RoadWidth * 0.5f;

            foreach (ObstacleSpot obstacle in layout.Obstacles)
            {
                foreach (MapRoad road in layout.Roads)
                {
                    Assert.That(MapGeometry.DistanceToPolyline(obstacle.Position, road.Points),
                        Is.GreaterThanOrEqualTo(roadHalfWidth), "An asteroid blocks a road.");
                }
            }

            foreach (ZoneSpot zone in layout.Zones)
            {
                Assert.That(IsRoadEndpoint(layout, zone.Center), Is.True, $"Zone at {zone.Center} has no road.");
            }

            foreach (SiteSpot site in layout.Sites)
            {
                Assert.That(IsRoadEndpoint(layout, site.Center), Is.True, $"Site at {site.Center} has no road.");
            }
        }

        [Test]
        public void SameSeed_ProducesSameLayout()
        {
            MapLayout first = Generate(MapSize.Medium, EnemyAiDifficulty.Hard, 42);
            MapLayout second = Generate(MapSize.Medium, EnemyAiDifficulty.Hard, 42);

            Assert.That(second.GetStationPosition(FactionType.Republic),
                Is.EqualTo(first.GetStationPosition(FactionType.Republic)));
            Assert.That(second.Obstacles.Count, Is.EqualTo(first.Obstacles.Count));
            Assert.That(second.PlanetPosition, Is.EqualTo(first.PlanetPosition));
        }

        private MapLayout Generate(MapSize mapSize, EnemyAiDifficulty difficulty, int seed)
        {
            return _generator.Generate(
                mapSize, difficulty, FactionType.Republic, FactionType.Separatist, new Random(seed));
        }

        private static float GetClosestMiningDistance(MapLayout layout)
        {
            Vector3 station = layout.GetStationPosition(FactionType.Republic);
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

        private static bool IsRoadEndpoint(MapLayout layout, Vector3 position)
        {
            foreach (MapRoad road in layout.Roads)
            {
                if (MapGeometry.Distance(road.Points[0], position) < TOLERANCE ||
                    MapGeometry.Distance(road.Points[road.Points.Count - 1], position) < TOLERANCE)
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
