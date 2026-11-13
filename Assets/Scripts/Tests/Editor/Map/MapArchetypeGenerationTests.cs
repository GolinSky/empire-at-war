using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Map.Generation;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Utils;
using EmpireAtWar.Views.ReinforcementZones;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

namespace EmpireAtWar.Tests.Editor
{
    [TestFixture(false, Category = "MapGeneration", Explicit = true)]
    [TestFixture(true, Category = "MapExhaustive", Explicit = true)]
    public sealed class MapArchetypeGenerationTests
    {
        private readonly bool _exhaustive;
        private MapGenerationSettings _settings;
        private MapLayoutGenerator _generator;

        public MapArchetypeGenerationTests(bool exhaustive) => _exhaustive = exhaustive;

        [SetUp]
        public void SetUp()
        {
            _settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(
                "Assets/Settings/Data/Models/Map/MapGenerationSettings.asset");
            CaptureSiteData sites = AssetDatabase.LoadAssetAtPath<CaptureSiteData>(
                "Assets/Settings/Data/Models/CaptureSites/CaptureSiteData.asset");
            CaptureSiteView mine = AssetDatabase.LoadAssetAtPath<CaptureSiteView>(
                "Assets/Prefabs/View/CaptureSites/CaptureSite.prefab");
            float radius = sites.GetSettings(CaptureSiteKind.Mining).Radius *
                Mathf.Max(Mathf.Abs(mine.transform.lossyScale.x), Mathf.Abs(mine.transform.lossyScale.z));
            _generator = new MapLayoutGenerator(_settings, new MapFeatureRadii(
                AssetDatabase.LoadAssetAtPath<ReinforcementZoneView>(
                    "Assets/Prefabs/View/ReinforcementZones/ReinforcementZone.prefab").Radius, radius,
                AssetDatabase.LoadAssetAtPath<ReinforcementZoneView>(
                    "Assets/Prefabs/View/ReinforcementZones/MainReinforcementZone.prefab").Radius));
        }

        public static IEnumerable<TestCaseData> Cases()
        {
            PlanetType[] planets = { PlanetType.Coruscant, PlanetType.Hoth, PlanetType.Mandalore,
                PlanetType.Endor, PlanetType.Kessel, PlanetType.Dagobah, PlanetType.Kamino };
            foreach (PlanetType planet in planets)
            foreach (MapSize size in Enum.GetValues(typeof(MapSize)))
            for (int count = 2; count <= MatchRules.GetMaxPlayers(size); count++)
                yield return new TestCaseData(planet, size, count);
        }

        [TestCaseSource(nameof(Cases))]
        public void Archetypes_SupportEveryAllowedRoster(PlanetType planet, MapSize mapSize, int count)
        {
            int[] seeds = _exhaustive ? Enumerable.Range(0, 20).ToArray() : new[] { 0, 7, 19 };
            foreach (int seed in seeds)
            {
                PlayerSlot[] players = CreatePlayers(count, seed % 2 == 1);
                MapLayout layout = _generator.Generate(mapSize, players, new Random(seed), seed, planet);
                string context = $"{planet}/{mapSize}/{count}/seed={seed}";
                Assert.That(layout.Zones.Count(zone => zone.IsMain), Is.EqualTo(1), context);
                foreach (PlayerSlot player in players)
                {
                    Vector3 station = layout.GetStationPosition(player.Id);
                    AssertInside(layout, station, context);
                    foreach (PlayerSlot other in players)
                        if (other.Id != player.Id)
                            Assert.That(PlanarGeometry.Distance(station, layout.GetStationPosition(other.Id)),
                                Is.GreaterThan(_settings.GetStationRadius(player.Faction) +
                                    _settings.GetStationRadius(other.Faction)), context);
                }
                for (int home = 0; home < count; home++)
                    Assert.That(layout.Sites.Count(site => site.PointId == home), Is.EqualTo(2),
                        context + " home mine and satellite");
                foreach (ZoneSpot zone in layout.Zones)
                {
                    AssertInside(layout, zone.Center, context);
                    AssertEndpoint(layout, zone.Center, context);
                }
                foreach (SiteSpot site in layout.Sites)
                {
                    AssertInside(layout, site.Center, context);
                    AssertEndpoint(layout, site.Center, context);
                }
                MapSizeSettings size = _settings.GetSize(mapSize);
                float clearance = (Mathf.Min(size.MinLaneWidth, size.PocketEntranceWidth) - size.FieldCellSize) * 0.5f;
                float minimumClearance = float.PositiveInfinity;
                foreach (AsteroidField field in layout.Fields)
                foreach (FieldVolume volume in field.Volumes)
                foreach (MapLane lane in layout.Lanes)
                    minimumClearance = Mathf.Min(minimumClearance,
                        PlanarGeometry.DistanceToPolyline(volume.Center, lane.Points) - volume.Radius);
                Assert.That(minimumClearance, Is.GreaterThanOrEqualTo(clearance - 0.1f), context + " blocked lane");
            }
        }

        [Test]
        public void TwinPass_SeparatesBoundarySlotsWhenContiguousTeamsCannotFitRelays()
        {
            PlayerSlot[] players = CreatePlayers(4, false);
            MapLayout layout = _generator.Generate(MapSize.Small, players, new Random(0), 0, PlanetType.Mandalore);
            MapSizeSettings size = _settings.GetSize(MapSize.Small).ForArchetype(
                _settings.Profiles.Get(PlanetType.Mandalore), players.Length);
            Quaternion inverse = Quaternion.Euler(0f, -size.SkeletonAngle, 0f);
            Vector3 center = PlanarGeometry.GetCenter(size.Bounds);
            // The failing grouped order is 0,2 | 1,3. The nearest boundary exchange is 0,1 | 2,3.
            Assert.That((inverse * (layout.GetStationPosition(players[0].Id) - center)).x, Is.LessThan(0f));
            Assert.That((inverse * (layout.GetStationPosition(players[1].Id) - center)).x, Is.LessThan(0f));
            Assert.That((inverse * (layout.GetStationPosition(players[2].Id) - center)).x, Is.GreaterThan(0f));
            Assert.That((inverse * (layout.GetStationPosition(players[3].Id) - center)).x, Is.GreaterThan(0f));
            foreach (SiteSpot site in layout.Sites)
            {
                if (site.PointId < 0 || site.PointId >= players.Length) continue;
                PlayerSlot owner = players[site.PointId];
                foreach (PlayerSlot enemy in players)
                    if (enemy.Team != owner.Team)
                        Assert.That(PlanarGeometry.Distance(site.Center, layout.GetStationPosition(enemy.Id)),
                            Is.GreaterThanOrEqualTo(size.AdditionalZoneStationDistance));
            }
            foreach (ZoneSpot zone in layout.Zones)
            {
                if (zone.IsCapturable) continue;
                PlayerSlot owner = players.Single(player => player.Id == zone.Owner);
                foreach (PlayerSlot enemy in players)
                    if (enemy.Team != owner.Team)
                        Assert.That(PlanarGeometry.Distance(zone.Center, layout.GetStationPosition(enemy.Id)),
                            Is.GreaterThanOrEqualTo(size.AdditionalZoneStationDistance));
            }
        }

        [Test]
        public void Profiles_CoverEveryPlanetExactlyOnce()
        {
            PlanetType[] planets = (PlanetType[])Enum.GetValues(typeof(PlanetType));
            Assert.That(_settings.Profiles.Entries.Count, Is.EqualTo(planets.Length));
            foreach (PlanetType planet in planets)
            {
                Assert.That(_settings.Profiles.Get(planet).Archetype, Is.Not.Null, planet.ToString());
                Assert.That(_settings.Profiles.Get(planet).Preview, Is.Not.Null, planet + " gallery preview");
            }
            Assert.That(_settings.Profiles.Entries.Select(entry => entry.Archetype.Kind).Distinct().Count(), Is.EqualTo(7));
        }

        [TestCase(PlanetType.Hoth, MapSize.Small, 4, 3)]
        [TestCase(PlanetType.Kessel, MapSize.Medium, 6, 4)]
        [TestCase(PlanetType.Endor, MapSize.Large, 8, 5)]
        [TestCase(PlanetType.Kamino, MapSize.Medium, 6, 2)]
        public void UnevenAndMultipleTeams_Generate(PlanetType planet, MapSize size, int count, int firstTeamSize)
        {
            PlayerSlot[] players = new PlayerSlot[count];
            for (int i = 0; i < count; i++)
            {
                int team = i < firstTeamSize ? 0 : firstTeamSize == 2 ? 1 + (i - firstTeamSize) / 2 : 1;
                players[i] = new PlayerSlot(new PlayerId(i), new TeamId(team),
                    i % 2 == 0 ? FactionType.Republic : FactionType.Separatist,
                    i == 0 ? PlayerController.Human : PlayerController.Ai, EnemyAiDifficulty.Medium, i);
            }
            MapLayout layout = _generator.Generate(size, players, new Random(19), 19, planet);
            foreach (PlayerSlot player in players)
                AssertInside(layout, layout.GetStationPosition(player.Id), planet.ToString());
            Assert.That(layout.Zones.Count(zone => !zone.IsCapturable), Is.EqualTo(count));
        }

        [Test]
        public void MissingProfile_FailsExplicitly()
        {
            PlanetMapProfile empty = ScriptableObject.CreateInstance<PlanetMapProfile>();
            try { Assert.Throws<InvalidOperationException>(() => empty.Get(PlanetType.Coruscant)); }
            finally { UnityEngine.Object.DestroyImmediate(empty); }
        }

        private static PlayerSlot[] CreatePlayers(int count, bool freeForAll)
        {
            PlayerSlot[] players = new PlayerSlot[count];
            for (int i = 0; i < count; i++)
                players[i] = new PlayerSlot(new PlayerId(i), new TeamId(freeForAll ? i : i % 2),
                    i % 2 == 0 ? FactionType.Republic : FactionType.Separatist,
                    i == 0 ? PlayerController.Human : PlayerController.Ai, EnemyAiDifficulty.Medium, i);
            return players;
        }

        private static void AssertInside(MapLayout layout, Vector3 point, string context)
        {
            Assert.That(point.x, Is.InRange(layout.SizeRange.Min.x, layout.SizeRange.Max.x), context);
            Assert.That(point.z, Is.InRange(layout.SizeRange.Min.y, layout.SizeRange.Max.y), context);
        }

        private static void AssertEndpoint(MapLayout layout, Vector3 point, string context)
        {
            Assert.That(layout.Lanes.Any(lane => PlanarGeometry.Distance(lane.Points[0], point) < 1f ||
                PlanarGeometry.Distance(lane.Points[lane.Points.Count - 1], point) < 1f), Is.True, context);
        }
    }
}
