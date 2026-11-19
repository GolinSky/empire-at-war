using System;
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
        private const float TEAM_SEPARATION_RATIO = 1.5f;

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

        // Twin Pass splits flanks by count, not team, so it is outside this rule.
        [TestCase(MapSize.Medium, 2, 2)]
        [TestCase(MapSize.Medium, 3, 2)]
        [TestCase(MapSize.Medium, 2, 3)]
        [TestCase(MapSize.Large, 2, 2)]
        [TestCase(MapSize.Large, 3, 2)]
        [TestCase(MapSize.Large, 4, 2)]
        [TestCase(MapSize.Large, 2, 3)]
        public void GroupedRingSlots_KeepEnemiesFartherThanAllies(MapSize mapSize, int teamSize, int teamCount)
        {
            MapGenerationSettings settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(SETTINGS_PATH);
            StationPlacer placer = new StationPlacer(settings, new MapFeatureRadii(270f, 120f, 540f));
            int[] teamSizes = new int[teamCount];
            for (int i = 0; i < teamCount; i++) teamSizes[i] = teamSize;
            PlayerSlot[] players = CreateTeams(teamSizes);
            HashSet<MapArchetypeKind> covered = new HashSet<MapArchetypeKind>();
            foreach (PlanetType planet in Enum.GetValues(typeof(PlanetType)))
            {
                PlanetMapProfileEntry profile = settings.Profiles.Get(planet);
                if (profile.Archetype.Kind == MapArchetypeKind.TwinPass ||
                    players.Length > profile.Archetype.MaximumPlayers || !covered.Add(profile.Archetype.Kind)) continue;
                MapSizeSettings size = settings.GetSize(mapSize).ForArchetype(profile, players.Length);
                Vector3 center = PlanarGeometry.GetCenter(size.Bounds);
                Vector3 mainDirection = Quaternion.Euler(0f, size.SkeletonAngle, 0f) * Vector3.forward;
                for (int seed = 0; seed < 20; seed++)
                {
                    string context = $"{profile.Archetype.Kind} seed {seed}";
                    MapStation[] stations = placer.PlaceSlots(size, players, new Random(seed), size.PlanetSeed, true);
                    Assert.That(placer.HasStationClearance(size, stations, players), Is.True, context);
                    AssertTeamsSeparated(stations, players, context);
                    AssertTeamsContiguous(stations, players, center, teamCount, context);
                    if (profile.Archetype.Kind != MapArchetypeKind.OrbitalRing) continue;
                    foreach (MapStation station in stations)
                    {
                        Vector3 direction = station.Position - center;
                        direction.y = 0f;
                        Assert.That(Vector3.Angle(direction, mainDirection), Is.GreaterThanOrEqualTo(58f), context);
                    }
                }
            }
            Assert.That(covered, Has.Member(MapArchetypeKind.OrbitalRing));
        }

        [TestCase(MapSize.Medium, 2, 2)]
        [TestCase(MapSize.Medium, 2, 1)]
        [TestCase(MapSize.Large, 2, 2)]
        [TestCase(MapSize.Large, 2, 1)]
        public void GroupedCorners_KeepEnemiesFartherAndOnlyMoveInward(MapSize mapSize, int firstTeam, int secondTeam)
        {
            MapGenerationSettings settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(SETTINGS_PATH);
            MapSizeSettings size = settings.GetSize(mapSize);
            StationPlacer placer = new StationPlacer(settings, new MapFeatureRadii(270f, 120f, 540f));
            PlayerSlot[] players = CreateTeams(firstTeam, secondTeam);
            Vector3 center = PlanarGeometry.GetCenter(size.Bounds);
            for (int seed = 0; seed < 20; seed++)
            {
                string context = $"Seed {seed}";
                MapStation[] grouped = placer.Place(size, players, new Random(seed), true);
                MapStation[] corners = placer.Place(size, players, new Random(seed));
                AssertTeamsSeparated(grouped, players, context);
                for (int i = 0; i < grouped.Length; i++)
                {
                    Vector3 pulled = grouped[i].Position - center;
                    Vector3 corner = corners[i].Position - center;
                    // Sliding along the shared edge keeps the corner quadrant and the outward home-site room.
                    Assert.That(Mathf.Sign(pulled.x), Is.EqualTo(Mathf.Sign(corner.x)), context);
                    Assert.That(Mathf.Sign(pulled.z), Is.EqualTo(Mathf.Sign(corner.z)), context);
                    Assert.That(Mathf.Abs(pulled.x), Is.LessThanOrEqualTo(Mathf.Abs(corner.x) + 0.01f), context);
                    Assert.That(Mathf.Abs(pulled.z), Is.LessThanOrEqualTo(Mathf.Abs(corner.z) + 0.01f), context);
                    Assert.That(Mathf.Min(Mathf.Abs(pulled.x - corner.x), Mathf.Abs(pulled.z - corner.z)),
                        Is.LessThan(0.01f), context + " one axis only");
                }
            }
        }

        [TestCase(MapSize.Medium)]
        [TestCase(MapSize.Large)]
        public void Grouping_LeavesRostersWithoutAlliesUnchanged(MapSize mapSize)
        {
            MapGenerationSettings settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(SETTINGS_PATH);
            StationPlacer placer = new StationPlacer(settings, new MapFeatureRadii(270f, 120f, 540f));
            for (int count = 2; count <= 4; count++)
            {
                int[] teamSizes = new int[count];
                for (int i = 0; i < count; i++) teamSizes[i] = 1;
                PlayerSlot[] players = CreateTeams(teamSizes);
                MapSizeSettings corners = settings.GetSize(mapSize);
                MapSizeSettings ring = corners.ForArchetype(settings.Profiles.Get(PlanetType.Endor), count);
                for (int seed = 0; seed < 5; seed++)
                {
                    MapStation[] groupedCorners = placer.Place(corners, players, new Random(seed), true);
                    MapStation[] plainCorners = placer.Place(corners, players, new Random(seed));
                    MapStation[] groupedSlots = placer.PlaceSlots(ring, players, new Random(seed), ring.PlanetSeed, true);
                    MapStation[] plainSlots = placer.PlaceSlots(ring, players, new Random(seed), ring.PlanetSeed);
                    for (int i = 0; i < count; i++)
                    {
                        Assert.That(groupedCorners[i].Position, Is.EqualTo(plainCorners[i].Position),
                            $"{count} corners seed {seed}");
                        Assert.That(groupedSlots[i].Position, Is.EqualTo(plainSlots[i].Position),
                            $"{count} slots seed {seed}");
                    }
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

        private static void AssertTeamsSeparated(IReadOnlyList<MapStation> stations, IReadOnlyList<PlayerSlot> players,
            string context)
        {
            foreach (MapStation station in stations)
            {
                TeamId team = players[station.Owner.Index].Team;
                float ally = float.MaxValue;
                float enemy = float.MaxValue;
                foreach (MapStation other in stations)
                {
                    if (other.Owner == station.Owner) continue;
                    float distance = PlanarGeometry.Distance(station.Position, other.Position);
                    if (players[other.Owner.Index].Team == team) ally = Mathf.Min(ally, distance);
                    else enemy = Mathf.Min(enemy, distance);
                }
                if (ally < float.MaxValue)
                    Assert.That(enemy, Is.GreaterThanOrEqualTo(TEAM_SEPARATION_RATIO * ally - 0.01f),
                        $"{context} player {station.Owner}");
            }
        }

        // Walking the ring by angle crosses each team boundary once: no enemy sits between two allies.
        private static void AssertTeamsContiguous(IReadOnlyList<MapStation> stations, IReadOnlyList<PlayerSlot> players,
            Vector3 center, int teamCount, string context)
        {
            List<MapStation> byAngle = new List<MapStation>(stations);
            byAngle.Sort((first, second) => Mathf.Atan2(first.Position.z - center.z, first.Position.x - center.x)
                .CompareTo(Mathf.Atan2(second.Position.z - center.z, second.Position.x - center.x)));
            int boundaries = 0;
            for (int i = 0; i < byAngle.Count; i++)
                if (players[byAngle[i].Owner.Index].Team != players[byAngle[(i + 1) % byAngle.Count].Owner.Index].Team)
                    boundaries++;
            Assert.That(boundaries, Is.EqualTo(teamCount), context);
        }

        private static PlayerSlot[] CreateTeams(params int[] teamSizes)
        {
            List<PlayerSlot> players = new List<PlayerSlot>();
            for (int team = 0; team < teamSizes.Length; team++)
            for (int member = 0; member < teamSizes[team]; member++)
                players.Add(new PlayerSlot(new PlayerId(players.Count), new TeamId(team), FactionType.Republic,
                    players.Count == 0 ? PlayerController.Human : PlayerController.Ai, EnemyAiDifficulty.Medium,
                    players.Count));
            return players.ToArray();
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
