using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Map.Generation;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using StationEntity = EmpireAtWar.Entities.SpaceStation.SpaceStation;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Map")]
    public sealed class StationAreaClearanceTests
    {
        [TestCase(FactionType.Republic)]
        [TestCase(FactionType.Empire)]
        [TestCase(FactionType.Rebellion)]
        [TestCase(FactionType.Separatist)]
        public void StationPrefab_BindsItsOnlySolidColliderAsNavigationHull(FactionType faction)
        {
            string path = $"Assets/Prefabs/Models/Stations/{faction}/{faction}SpaceStationView.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            StationEntity station = prefab.GetComponent<StationEntity>();
            Collider[] colliders = prefab.GetComponentsInChildren<Collider>(true);

            Assert.That(colliders, Has.Length.EqualTo(1));
            Assert.That(station.HullCollider, Is.SameAs(colliders[0]));
            Assert.That(station.HullCollider.isTrigger, Is.False);
            Assert.That(station.HullCollider.enabled, Is.True);
        }

        [Test]
        public void StationFootprint_TracksColliderHeightChangesAtWorldPosition()
        {
            MapGenerationSettings settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>(
                "Assets/Settings/Data/Models/Map/MapGenerationSettings.asset");
            GameObject station = new GameObject("Station hull");
            try
            {
                station.transform.position = new Vector3(0f, -40f, 0f);
                BoxCollider hull = station.AddComponent<BoxCollider>();
                hull.center = new Vector3(0f, 5f, 0f);
                hull.size = new Vector3(100f, 20f, 100f);
                var facade = new StationFootprintFacade(settings, FactionType.Republic, hull);

                Assert.That(facade.NavigationRadius, Is.EqualTo(settings.GetStationRadius(FactionType.Republic)));
                Assert.That(facade.HullBottom, Is.EqualTo(-45f));
                Assert.That(facade.HullTop, Is.EqualTo(-25f));

                hull.size = new Vector3(100f, 40f, 100f);
                Assert.That(facade.HullBottom, Is.EqualTo(-55f));
                Assert.That(facade.HullTop, Is.EqualTo(-15f));
            }
            finally
            {
                Object.DestroyImmediate(station);
            }
        }

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
