using System.Collections.Generic;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Map.Generation;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Utils;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Map")]
    public sealed class MapStationSlotTests
    {
        private const string SETTINGS_PATH = "Assets/Settings/Data/Models/Map/MapGenerationSettings.asset";

        [TestCase(MapSize.Small, 2)]
        [TestCase(MapSize.Small, 3)]
        [TestCase(MapSize.Small, 4)]
        [TestCase(MapSize.Medium, 5)]
        [TestCase(MapSize.Medium, 6)]
        [TestCase(MapSize.Large, 7)]
        [TestCase(MapSize.Large, 8)]
        public void Slots_KeepEqualCenterDistancesAndSeparateAllPlayers(MapSize mapSize, int count)
        {
            MapGenerationSettings settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(SETTINGS_PATH);
            MapSizeSettings size = settings.GetSize(mapSize);
            StationPlacer placer = new StationPlacer(settings, new MapFeatureRadii(270f, 120f, 540f));
            PlayerSlot[] players = CreatePlayers(count);
            Vector3 center = PlanarGeometry.GetCenter(size.Bounds);
            for (int seed = 0; seed < 20; seed++)
            {
                MapStation[] stations = placer.PlaceSlots(size, players, new Random(seed), 137);
                MapStation[] repeated = placer.PlaceSlots(size, players, new Random(seed), 137);
                Assert.That(stations.Length, Is.EqualTo(count));
                Assert.That(placer.HasStationClearance(size, stations, players), Is.True, $"Seed {seed}");
                float distance = PlanarGeometry.Distance(center, stations[0].Position);
                for (int i = 0; i < count; i++)
                {
                    MapStation station = stations[i];
                    Assert.That(station.Position, Is.EqualTo(repeated[i].Position));
                    Assert.That(PlanarGeometry.Distance(center, station.Position), Is.EqualTo(distance).Within(0.01f));
                    Assert.That(station.Position.x - station.Radius, Is.GreaterThanOrEqualTo(size.Bounds.Min.x));
                    Assert.That(station.Position.x + station.Radius, Is.LessThanOrEqualTo(size.Bounds.Max.x));
                    Assert.That(station.Position.z - station.Radius, Is.GreaterThanOrEqualTo(size.Bounds.Min.y));
                    Assert.That(station.Position.z + station.Radius, Is.LessThanOrEqualTo(size.Bounds.Max.y));
                }
                if (count % 2 == 0)
                    for (int i = 0; i < count / 2; i++)
                        Assert.That(PlanarGeometry.Distance(PlanarGeometry.Reflect(stations[i].Position, center),
                            stations[i + count / 2].Position), Is.LessThan(0.01f));
            }
        }

        [TestCase(MapSize.Small, 3)]
        [TestCase(MapSize.Small, 4)]
        [TestCase(MapSize.Medium, 5)]
        [TestCase(MapSize.Medium, 6)]
        [TestCase(MapSize.Large, 7)]
        [TestCase(MapSize.Large, 8)]
        public void OrbitalSlots_LeaveGapForOffsetMainRelay(MapSize mapSize, int count)
        {
            MapGenerationSettings settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(SETTINGS_PATH);
            MapSizeSettings size = settings.GetSize(mapSize).ForArchetype(settings.Profiles.Get(PlanetType.Endor), count);
            MapFeatureRadii radii = new MapFeatureRadii(270f, 120f, 540f);
            StationPlacer placer = new StationPlacer(settings, radii);
            PlayerSlot[] players = CreatePlayers(count);
            Vector3 center = PlanarGeometry.GetCenter(size.Bounds);
            Vector3 mainDirection = Quaternion.Euler(0f, size.SkeletonAngle, 0f) * Vector3.forward;
            for (int seed = 0; seed < 20; seed++)
            {
                MapStation[] stations = placer.PlaceSlots(size, players, new Random(seed), size.PlanetSeed);
                Assert.That(placer.HasStationClearance(size, stations, players), Is.True, $"Seed {seed}");
                foreach (MapStation station in stations)
                {
                    Vector3 direction = station.Position - center;
                    direction.y = 0f;
                    Assert.That(Vector3.Angle(direction, mainDirection),
                        Is.GreaterThanOrEqualTo(58f), $"Seed {seed}");
                }
            }
        }

        [TestCase(MapSize.Small, 2)]
        [TestCase(MapSize.Small, 3)]
        [TestCase(MapSize.Small, 4)]
        [TestCase(MapSize.Medium, 5)]
        [TestCase(MapSize.Medium, 6)]
        [TestCase(MapSize.Large, 7)]
        [TestCase(MapSize.Large, 8)]
        public void TwinPassSlots_UseSeparatedFlanksWithContiguousTeamOrder(MapSize mapSize, int count)
        {
            MapGenerationSettings settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(SETTINGS_PATH);
            MapSizeSettings size = settings.GetSize(mapSize).ForArchetype(settings.Profiles.Get(PlanetType.Mandalore), count);
            StationPlacer placer = new StationPlacer(settings, new MapFeatureRadii(270f, 120f, 540f));
            PlayerSlot[] players = CreatePlayers(count);
            Vector3 center = PlanarGeometry.GetCenter(size.Bounds);
            Quaternion inverse = Quaternion.Euler(0f, -size.SkeletonAngle, 0f);
            for (int seed = 0; seed < 20; seed++)
            {
                MapStation[] stations = placer.PlaceSlots(size, players, new Random(seed), size.PlanetSeed);
                Assert.That(placer.HasStationClearance(size, stations, players), Is.True, $"Seed {seed}");
                int previousTeam = -1;
                for (int i = 0; i < stations.Length; i++)
                {
                    MapStation station = stations[i];
                    Vector3 local = inverse * (station.Position - center);
                    Assert.That(Mathf.Abs(local.x), Is.GreaterThan(Mathf.Abs(local.z)), $"Seed {seed}");
                    Assert.That(local.x < 0f, Is.EqualTo(i < (count + 1) / 2));
                    int team = players[station.Owner.Index].Team.Value;
                    Assert.That(team, Is.GreaterThanOrEqualTo(previousTeam));
                    previousTeam = team;
                    Assert.That(station.Position.x - station.Radius, Is.GreaterThanOrEqualTo(size.Bounds.Min.x));
                    Assert.That(station.Position.x + station.Radius, Is.LessThanOrEqualTo(size.Bounds.Max.x));
                    Assert.That(station.Position.z - station.Radius, Is.GreaterThanOrEqualTo(size.Bounds.Min.y));
                    Assert.That(station.Position.z + station.Radius, Is.LessThanOrEqualTo(size.Bounds.Max.y));
                }
            }
        }

        [Test]
        public void Fairness_RejectsDisconnectedAndUnequalLaneAccess()
        {
            PlayerSlot[] players = CreatePlayers(2);
            MapStation[] stations =
            {
                new MapStation(FactionType.Republic, players[0].Id, new Vector3(-120f, 0f, 0f), 10f),
                new MapStation(FactionType.Republic, players[1].Id, new Vector3(120f, 0f, 0f), 10f)
            };
            List<MapNode> nodes = new List<MapNode>
            {
                new MapNode(MapNodeKind.DefaultZone, new Vector3(-100f, 0f, 0f), players[0].Id, 5f),
                new MapNode(MapNodeKind.DefaultZone, new Vector3(100f, 0f, 0f), players[1].Id, 5f),
                new MapNode(MapNodeKind.CapturableZone, Vector3.zero, PlayerId.None, 5f) { IsMain = true },
                new MapNode(MapNodeKind.MiningSite, new Vector3(0f, 0f, 10f), PlayerId.None, 5f)
            };
            List<MapLane> lanes = new List<MapLane>
            {
                new MapLane(new[] { nodes[0].Center, nodes[2].Center }),
                new MapLane(new[] { nodes[1].Center, nodes[2].Center }),
                new MapLane(new[] { nodes[2].Center, nodes[3].Center })
            };
            Assert.That(MapLayoutFairness.IsFair(stations, players, nodes, lanes), Is.True);
            lanes[1] = new MapLane(new[] { nodes[1].Center, new Vector3(500f, 0f, 500f), nodes[2].Center });
            Assert.That(MapLayoutFairness.IsFair(stations, players, nodes, lanes), Is.False);
            lanes.RemoveAt(1);
            Assert.That(MapLayoutFairness.IsFair(stations, players, nodes, lanes), Is.False);
        }

        [Test]
        public void Fairness_CoincidentRelayAndMineShareLaneAccess()
        {
            PlayerSlot[] players = CreatePlayers(2);
            MapStation[] stations =
            {
                new MapStation(FactionType.Republic, players[0].Id, new Vector3(-120f, 0f, 0f), 10f),
                new MapStation(FactionType.Republic, players[1].Id, new Vector3(120f, 0f, 0f), 10f)
            };
            List<MapNode> nodes = new List<MapNode>
            {
                new MapNode(MapNodeKind.DefaultZone, new Vector3(-100f, 0f, 0f), players[0].Id, 5f),
                new MapNode(MapNodeKind.DefaultZone, new Vector3(100f, 0f, 0f), players[1].Id, 5f),
                new MapNode(MapNodeKind.CapturableZone, Vector3.zero, PlayerId.None, 5f) { IsMain = true },
                new MapNode(MapNodeKind.CapturableZone, new Vector3(0f, 0f, 30f), PlayerId.None, 5f),
                new MapNode(MapNodeKind.MiningSite, new Vector3(0f, 0f, 30f), PlayerId.None, 5f)
            };
            List<MapLane> lanes = new List<MapLane>
            {
                new MapLane(new[] { nodes[0].Center, nodes[2].Center }),
                new MapLane(new[] { nodes[1].Center, nodes[2].Center }),
                new MapLane(new[] { nodes[2].Center, nodes[3].Center })
            };
            Assert.That(MapLayoutFairness.IsFair(stations, players, nodes, lanes), Is.True,
                "A mine sharing a relay center must not become unreachable because the endpoint resolves to its relay.");
            lanes.RemoveAt(2);
            Assert.That(MapLayoutFairness.IsFair(stations, players, nodes, lanes), Is.False,
                "Coincident nodes still require a connecting lane to the other capture points.");
        }

        private static PlayerSlot[] CreatePlayers(int count)
        {
            PlayerSlot[] players = new PlayerSlot[count];
            for (int i = 0; i < count; i++)
                players[i] = new PlayerSlot(new PlayerId(i), new TeamId(i % 2), FactionType.Republic,
                    i == 0 ? PlayerController.Human : PlayerController.Ai, EnemyAiDifficulty.Medium, i);
            return players;
        }
    }
}
