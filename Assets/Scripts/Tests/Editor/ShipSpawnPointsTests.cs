using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.Obstacles;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.ReinforcementZones;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Services.Reinforcement;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Services.ShipSpawning;
using EmpireAtWar.Services.SpawnBlocking;
using EmpireAtWar.Services.Vision;
using EmpireAtWar.Views.ReinforcementZones;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class ShipSpawnPointsTests
    {
        private const BindingFlags PRIVATE_INSTANCE =
            BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly Vector3 HOME = new Vector3(-3000f, 0f, 0f);
        private static readonly Vector3 RELAY = new Vector3(3000f, 0f, 0f);

        private GameObject _root;
        private ReinforcementZoneData _data;
        private PlayerRoster _roster;
        private VisionService _vision;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject(nameof(ShipSpawnPointsTests));
            _data = ScriptableObject.CreateInstance<ReinforcementZoneData>();
            _roster = TestPlayers.CreateTeamGame();
            _vision = new VisionService(_roster);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_data);
        }

        [Test]
        public void RandomSpawn_PrefersRelayHeldByOwnerTeam()
        {
            ShipSpawnPoints spawnPoints = CreateSpawnPoints(TestPlayers.Ally, new OpenEverywhere());

            Assert.That(spawnPoints.TryGetRandomSpawnPosition(
                TestPlayers.Human, ShipType.Arquitens, out Vector3 position), Is.True);
            Assert.That(PlanarDistance(position, RELAY), Is.LessThanOrEqualTo(_data.RelaySpawnBlockRadius));
        }

        [Test]
        public void RandomSpawn_RelayClosed_FallsBackToHome()
        {
            ShipSpawnPoints spawnPoints = CreateSpawnPoints(TestPlayers.Human,
                new OpenNear(HOME, _data.HomeAreaRadius));

            Assert.That(spawnPoints.TryGetRandomSpawnPosition(
                TestPlayers.Human, ShipType.Arquitens, out Vector3 position), Is.True);
            Assert.That(PlanarDistance(position, HOME), Is.LessThanOrEqualTo(_data.HomeAreaRadius));
        }

        [Test]
        public void RandomSpawn_HostileRelay_IsNeverAnAnchor()
        {
            ShipSpawnPoints spawnPoints = CreateSpawnPoints(TestPlayers.Enemy, new OpenEverywhere());

            Assert.That(spawnPoints.TryGetRandomSpawnPosition(
                TestPlayers.Human, ShipType.Arquitens, out Vector3 position), Is.True);
            Assert.That(PlanarDistance(position, HOME), Is.LessThanOrEqualTo(_data.HomeAreaRadius));
        }

        [Test]
        public void RandomSpawn_NothingOpen_ReturnsFalse()
        {
            ShipSpawnPoints spawnPoints = CreateSpawnPoints(TestPlayers.Human, new OpenNear(Vector3.zero, 1f));

            Assert.That(spawnPoints.TryGetRandomSpawnPosition(
                TestPlayers.Human, ShipType.Arquitens, out _), Is.False);
        }

        [Test]
        public void DefaultZoneSpawn_NoHomeForOwner_ReturnsFalse()
        {
            ShipSpawnPoints spawnPoints = CreateSpawnPoints(TestPlayers.Human, new OpenEverywhere());

            Assert.That(spawnPoints.TryGetDefaultZoneSpawnPosition(
                TestPlayers.SecondEnemy, ShipType.Arquitens, out _), Is.False);
        }

        [Test]
        public void Home_LightsItsWholeSpawnAreaForTheOwnerTeamOnly()
        {
            CreateSpawnPoints(TestPlayers.Human, new OpenEverywhere());
            Vector3 edge = HOME + Vector3.forward * _data.HomeAreaRadius;

            Assert.That(_vision.IsVisible(TestPlayers.Human, edge), Is.True);
            Assert.That(_vision.IsVisible(TestPlayers.Ally, edge), Is.True);
            Assert.That(_vision.IsVisible(TestPlayers.Enemy, edge), Is.False);
            Assert.That(_vision.IsVisible(TestPlayers.Human,
                HOME + Vector3.forward * (_data.HomeAreaRadius + 1f)), Is.False);
        }

        private ShipSpawnPoints CreateSpawnPoints(PlayerId relayOwner, IReinforcementSpawnRule rule)
        {
            ReinforcementZoneView relay = CreateRelay(relayOwner, RELAY);
            ReinforcementZonesSystem system = _root.AddComponent<ReinforcementZonesSystem>();
            SetField(system, "_data", _data);
            SetField(system, "_playerRoster", _roster);
            SetField(system, "_localPlayer", TestPlayers.CreateLocalPlayer(_roster));
            SetField(system, "_spawnBlockerService", new SpawnBlockerService(_roster));
            SetField(system, "_visionService", _vision);
            system.UpdateState(new BattleMap(
                layout: CreateLayout(new ZoneSpot(HOME, TestPlayers.Human, false),
                    new ZoneSpot(RELAY, relayOwner, true)),
                zoneViews: new[] { relay },
                siteViews: System.Array.Empty<CaptureSiteView>(),
                obstacles: System.Array.Empty<MapObstacle>(),
                stationObstacles: System.Array.Empty<StationObstacle>()));
            return new ShipSpawnPoints(system, system, rule, _roster, _data);
        }

        private static MapLayout CreateLayout(params ZoneSpot[] zones)
        {
            return new MapLayout(
                stationPositions: new Dictionary<PlayerId, Vector3>(),
                zones: zones,
                sites: System.Array.Empty<SiteSpot>(),
                lanes: System.Array.Empty<MapLane>(),
                fields: System.Array.Empty<AsteroidField>(),
                sizeRange: new Vector2Range(),
                planetPosition: Vector3.zero);
        }

        private ReinforcementZoneView CreateRelay(PlayerId owner, Vector3 center)
        {
            GameObject gameObject = new GameObject($"{owner}Relay");
            gameObject.transform.SetParent(_root.transform);
            gameObject.transform.position = center;
            ReinforcementZoneView view = gameObject.AddComponent<ReinforcementZoneView>();
            SetField(view, "sphereRenderer", gameObject.AddComponent<MeshRenderer>());
            GameObject captureUi = new GameObject("CaptureUi", typeof(RectTransform));
            captureUi.transform.SetParent(gameObject.transform);
            SetField(view, "captureCanvas", captureUi.AddComponent<Canvas>());
            SetField(view, "_startingOwner", owner);
            SetField(view, "isCapturable", true);
            return view;
        }

        private static float PlanarDistance(Vector3 first, Vector3 second) =>
            new Vector2(first.x - second.x, first.z - second.z).magnitude;

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, PRIVATE_INSTANCE);
            Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}' to exist.");
            field.SetValue(target, value);
        }

        private sealed class OpenEverywhere : IReinforcementSpawnRule
        {
            public bool IsOpen(PlayerId team, Vector3 position) => true;

            public bool CanSpawnShip(PlayerId owner, ShipType shipType, Vector3 position) => true;

            public bool CanSpawnStructure(PlayerId owner, Vector3 position) => true;
        }

        private sealed class OpenNear : IReinforcementSpawnRule
        {
            private readonly Vector3 _center;
            private readonly float _radius;

            public OpenNear(Vector3 center, float radius)
            {
                _center = center;
                _radius = radius;
            }

            public bool IsOpen(PlayerId team, Vector3 position) => PlanarDistance(position, _center) <= _radius;

            public bool CanSpawnShip(PlayerId owner, ShipType shipType, Vector3 position) => IsOpen(owner, position);

            public bool CanSpawnStructure(PlayerId owner, Vector3 position) => IsOpen(owner, position);
        }
    }
}
