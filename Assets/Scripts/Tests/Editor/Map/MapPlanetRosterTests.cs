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
using EmpireAtWar.Views.ReinforcementZones;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

namespace EmpireAtWar.Tests.Editor
{
    [TestFixture, Category("MapGeneration"), Explicit]
    public sealed class MapPlanetRosterTests
    {
        private const int RUN_SEED = 7;
        private const float NETWORK_POSITION_MIN = -8192f;
        private const float NETWORK_POSITION_MAX = 8191.75f;
        private MapGenerationSettings _settings;
        private MapLayoutGenerator _generator;

        public static IEnumerable<PlanetType> Planets() => Enum.GetValues(typeof(PlanetType)).Cast<PlanetType>();

        [SetUp]
        public void SetUp()
        {
            _settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(
                "Assets/Settings/Data/Models/Map/MapGenerationSettings.asset");
            CaptureSiteData sites = AssetDatabase.LoadAssetAtPath<CaptureSiteData>(
                "Assets/Settings/Data/Models/CaptureSites/CaptureSiteData.asset");
            CaptureSiteView mine = AssetDatabase.LoadAssetAtPath<CaptureSiteView>(
                "Assets/Prefabs/View/CaptureSites/CaptureSite.prefab");
            float miningRadius = sites.GetSettings(CaptureSiteKind.Mining).Radius *
                Mathf.Max(Mathf.Abs(mine.transform.lossyScale.x), Mathf.Abs(mine.transform.lossyScale.z));
            _generator = new MapLayoutGenerator(_settings, new MapFeatureRadii(
                AssetDatabase.LoadAssetAtPath<ReinforcementZoneView>(
                    "Assets/Prefabs/View/ReinforcementZones/ReinforcementZone.prefab").Radius,
                miningRadius,
                AssetDatabase.LoadAssetAtPath<ReinforcementZoneView>(
                    "Assets/Prefabs/View/ReinforcementZones/MainReinforcementZone.prefab").Radius));
        }

        [TestCaseSource(nameof(Planets))]
        public void EveryPlanet_SupportsEightPlayersInTwoTeams(PlanetType planet)
        {
            PlayerSlot[] players = new PlayerSlot[MatchRules.GetMaxPlayers(MapSize.Large)];
            for (int i = 0; i < players.Length; i++)
                players[i] = new PlayerSlot(new PlayerId(i), new TeamId(i % 2),
                    i % 2 == 0 ? FactionType.Republic : FactionType.Separatist,
                    i == 0 ? PlayerController.Human : PlayerController.Ai, EnemyAiDifficulty.Medium, i);
            MapLayout layout = _generator.Generate(MapSize.Large, players, new Random(RUN_SEED), RUN_SEED, planet);
            MapSizeSettings size = _settings.GetSize(MapSize.Large).ForArchetype(_settings.Profiles.Get(planet), players.Length);
            Assert.That(layout.SizeRange.Min.x, Is.GreaterThanOrEqualTo(NETWORK_POSITION_MIN));
            Assert.That(layout.SizeRange.Min.y, Is.GreaterThanOrEqualTo(NETWORK_POSITION_MIN));
            Assert.That(layout.SizeRange.Max.x, Is.LessThanOrEqualTo(NETWORK_POSITION_MAX));
            Assert.That(layout.SizeRange.Max.y, Is.LessThanOrEqualTo(NETWORK_POSITION_MAX));
            Assert.That(layout.Zones.Count(zone => zone.IsMain), Is.EqualTo(1));
            Assert.That(layout.Zones.Count, Is.EqualTo(players.Length + 1 + size.AdditionalZonePairCount * 2));
            Assert.That(layout.Hazards.Count, Is.EqualTo(size.HazardCount));
            foreach (PlayerSlot player in players)
            {
                Vector3 station = layout.GetStationPosition(player.Id);
                Assert.That(station.x, Is.InRange(layout.SizeRange.Min.x, layout.SizeRange.Max.x));
                Assert.That(station.z, Is.InRange(layout.SizeRange.Min.y, layout.SizeRange.Max.y));
            }
        }
    }
}
