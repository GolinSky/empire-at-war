using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Map.Generation;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Map")]
    public sealed class StationAreaClearanceTests
    {
        [TestCase(MapSize.Small, MapNodeKind.MiningSite)]
        [TestCase(MapSize.Medium, MapNodeKind.MiningSite)]
        [TestCase(MapSize.Large, MapNodeKind.MiningSite)]
        [TestCase(MapSize.Small, MapNodeKind.SatelliteSite)]
        [TestCase(MapSize.Small, MapNodeKind.DefaultZone)]
        public void EnemyMinimum_UsesHomeStationOrderAndIgnoresOwnTeam(MapSize mapSize, MapNodeKind kind)
        {
            MapGenerationSettings settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(
                "Assets/Settings/Data/Models/Map/MapGenerationSettings.asset");
            MapSizeSettings size = settings.GetSize(mapSize);
            PlayerSlot[] players =
            {
                Player(0, 0), Player(1, 1), Player(2, 1)
            };
            // Station order deliberately differs from lobby slot order; home PointId indexes stations.
            MapStation[] stations =
            {
                Station(players[1], 0f), Station(players[2], 200f), Station(players[0], 3000f)
            };
            bool home = kind != MapNodeKind.DefaultZone;
            MapNode friendly = new MapNode(kind, new Vector3(100f, 0f, 0f),
                home ? PlayerId.None : players[1].Id, 60f, 0) { IsHomeSite = home };
            Assert.That(StationAreaClearance.HasEnemyClearance(friendly, size, stations, players), Is.True,
                "Own and allied stations must not be subject to enemy distance.");
            MapNode nearEnemy = new MapNode(kind,
                new Vector3(3000f - size.AdditionalZoneStationDistance + 1f, 0f, 0f),
                home ? PlayerId.None : players[1].Id, 60f, 0) { IsHomeSite = home };
            Assert.That(StationAreaClearance.HasEnemyClearance(nearEnemy, size, stations, players), Is.False,
                "A non-overlapping site inside the enemy exclusion must be rejected.");
            MapNode boundary = new MapNode(kind,
                new Vector3(3000f - size.AdditionalZoneStationDistance, 0f, 0f),
                home ? PlayerId.None : players[1].Id, 60f, 0) { IsHomeSite = home };
            Assert.That(StationAreaClearance.HasEnemyClearance(boundary, size, stations, players), Is.True);
        }

        private static PlayerSlot Player(int id, int team) => new PlayerSlot(new PlayerId(id), new TeamId(team),
            FactionType.Republic, PlayerController.Ai, EnemyAiDifficulty.Medium, id);

        private static MapStation Station(PlayerSlot player, float x) =>
            new MapStation(player.Faction, player.Id, new Vector3(x, 0f, 0f), 100f);
    }
}
