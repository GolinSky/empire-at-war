using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Components.Obstacles;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Capture;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.ReinforcementZones;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Services.Enemy;
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
    [Category("Ships")]
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
        public void TargetedSpawn_HomeTarget_DoesNotChooseRemoteRelay()
        {
            ShipSpawnPoints spawnPoints = CreateSpawnPoints(TestPlayers.Ally, new OpenEverywhere());
            Assert.That(spawnPoints.TryGetSpawnPositionNear(TestPlayers.Human, ShipType.Arquitens,
                HOME, out Vector3 position), Is.True);
            Assert.That(PlanarDistance(position, HOME), Is.LessThanOrEqualTo(_data.HomeAreaRadius));
        }

        [Test]
        public void TargetedSpawn_FrontTarget_ChoosesAlliedRelay()
        {
            ShipSpawnPoints spawnPoints = CreateSpawnPoints(TestPlayers.Ally, new OpenEverywhere());
            Assert.That(spawnPoints.TryGetSpawnPositionNear(TestPlayers.Human, ShipType.Arquitens,
                RELAY, out Vector3 position), Is.True);
            Assert.That(PlanarDistance(position, RELAY), Is.LessThanOrEqualTo(_data.RelaySpawnBlockRadius));
        }

        [Test]
        public void TargetedSpawn_UsesCloserLivingStationBeforeRemoteHomeOrRelay()
        {
            var entities = new EntityLocator();
            Vector3 stationPosition = new Vector3(0f, 0f, 3000f);
            var stationObject = new GameObject("Task station");
            stationObject.transform.SetParent(_root.transform);
            stationObject.transform.position = stationPosition;
            var health = new StationHealth();
            var station = new Entity(health, entities, new IEntityFacade[]
            {
                new PlayerBaseFacade(), new EntityTransformFacade(stationObject.transform)
            }, TestPlayers.Human, 1, System.Array.Empty<IPooledEntityEvents>(),
                System.Array.Empty<IHangarLaunchEvents>());
            entities.AddEntity(station);
            ShipSpawnPoints spawnPoints = CreateSpawnPoints(TestPlayers.Ally, new OpenEverywhere(), entities);
            Assert.That(spawnPoints.TryGetSpawnPositionNear(TestPlayers.Human, ShipType.Arquitens,
                stationPosition, out Vector3 nearStation), Is.True);
            Assert.That(PlanarDistance(nearStation, stationPosition), Is.LessThanOrEqualTo(_data.HomeAreaRadius));

            health.IsDestroyed = true;
            Assert.That(spawnPoints.TryGetSpawnPositionNear(TestPlayers.Human, ShipType.Arquitens,
                stationPosition, out Vector3 awayFromStation), Is.True);
            Assert.That(PlanarDistance(awayFromStation, stationPosition), Is.GreaterThan(_data.HomeAreaRadius));
        }

        [Test]
        public void TargetedSpawn_ClosestAnchorBlocked_TriesNextHeldAnchor()
        {
            ShipSpawnPoints spawnPoints = CreateSpawnPoints(TestPlayers.Ally, new OpenNear(HOME, _data.HomeAreaRadius));
            Assert.That(spawnPoints.TryGetSpawnPositionNear(TestPlayers.Human, ShipType.Arquitens,
                RELAY, out Vector3 position), Is.True);
            Assert.That(PlanarDistance(position, HOME), Is.LessThanOrEqualTo(_data.HomeAreaRadius));
        }

        [Test]
        public void DefaultZoneSpawn_NoHomeForOwner_ReturnsFalse()
        {
            ShipSpawnPoints spawnPoints = CreateSpawnPoints(TestPlayers.Human, new OpenEverywhere());

            Assert.That(spawnPoints.TryGetDefaultZoneSpawnPosition(
                TestPlayers.SecondEnemy, ShipType.Arquitens, out _), Is.False);
        }

        [Test]
        public void DefenseSpawn_PrefersRearOfStationWhenBothSidesAreOpen()
        {
            ShipSpawnPoints spawnPoints = CreateSpawnPoints(TestPlayers.Ally, new OpenEverywhere());
            var threats = new PlacementThreatSnapshot();
            threats.Add(HOME.x + _data.HomeAreaRadius * 0.5f, HOME.z, _data.HomeAreaRadius * 0.25f);

            Assert.That(spawnPoints.TryGetDefenseSpawnPosition(TestPlayers.Human, ShipType.Arquitens,
                HOME, threats, out Vector3 position), Is.True);
            Assert.That(position.x, Is.LessThanOrEqualTo(HOME.x));
            Assert.That(PlanarDistance(position, HOME), Is.LessThanOrEqualTo(_data.HomeAreaRadius + 0.01f));
        }

        [Test]
        public void DefenseSpawn_WhenEnemyRangeCoversRally_RejectsOtherwiseOpenPositions()
        {
            ShipSpawnPoints spawnPoints = CreateSpawnPoints(TestPlayers.Ally, new OpenEverywhere());
            var threats = new PlacementThreatSnapshot();
            threats.Add(HOME.x + _data.HomeAreaRadius, HOME.z, _data.HomeAreaRadius * 2f + 1f);

            Assert.That(spawnPoints.TryGetDefenseSpawnPosition(TestPlayers.Human, ShipType.Arquitens,
                HOME, threats, out _), Is.False);
            Assert.That(spawnPoints.TryGetBestDefenseSpawnPosition(TestPlayers.Human, ShipType.Arquitens,
                HOME, threats, out Vector3 position), Is.True);
            Assert.That(position.x, Is.LessThanOrEqualTo(HOME.x));
            Assert.That(PlanarDistance(position, HOME), Is.LessThanOrEqualTo(_data.HomeAreaRadius + 0.01f));
        }

        [Test]
        public void RearSpawn_CrowdedInnerCircleAndFrontThreat_FindsOuterRearRing()
        {
            ShipSpawnPoints spawnPoints = CreateSpawnPoints(TestPlayers.Ally,
                new BlockedNear(HOME, _data.HomeAreaRadius));
            var threats = new PlacementThreatSnapshot();
            threats.Add(HOME.x + _data.HomeAreaRadius * 0.5f, HOME.z, _data.HomeAreaRadius * 1.25f);

            Assert.That(spawnPoints.TryGetRearSpawnPosition(TestPlayers.Human, ShipType.Arquitens,
                HOME, threats, requireThreatSafety: true, out Vector3 position), Is.True);
            float distance = PlanarDistance(position, HOME);
            Assert.That(distance, Is.GreaterThan(_data.HomeAreaRadius));
            Assert.That(distance, Is.LessThanOrEqualTo(_data.HomeAreaRadius * 3f + 0.01f));
            Assert.That(position.x, Is.LessThanOrEqualTo(HOME.x));
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

        private ShipSpawnPoints CreateSpawnPoints(PlayerId relayOwner, IReinforcementSpawnRule rule,
            IEntityLocator entities = null)
        {
            ReinforcementZoneView relay = CreateRelay(relayOwner, RELAY);
            ReinforcementZonesSystem system = _root.AddComponent<ReinforcementZonesSystem>();
            SetField(system, "_data", _data);
            SetField(system, "_playerRoster", _roster);
            SetField(system, "_localPlayer", TestPlayers.CreateLocalPlayer(_roster));
            SetField(system, "_areas", new BattleAreaService(_roster));
            SetField(system, "_visionService", _vision);
            SetField(system, "_strengthSampler", new CaptureStrengthSampler(null, null, _roster, _data.SquadronCaptureWeight));
            system.UpdateState(new BattleMap(
                layout: CreateLayout(new ZoneSpot(HOME, TestPlayers.Human, false),
                    new ZoneSpot(RELAY, relayOwner, true)),
                zoneViews: new[] { relay },
                siteViews: System.Array.Empty<CaptureSiteView>(),
                obstacles: System.Array.Empty<MapObstacle>(),
                stationObstacles: System.Array.Empty<StationObstacle>()));
            return new ShipSpawnPoints(system, system, rule, _roster, _data, entities ?? new EntityLocator(), null);
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
            CaptureRingView ring = gameObject.AddComponent<CaptureRingView>();
            SetField(ring, "ringRenderer", gameObject.AddComponent<MeshRenderer>());
            SetField(view, "captureRing", ring);
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

        private sealed class StationHealth : IHealthModelObserver
        {
            public event System.Action OnDestroy { add { } remove { } }
            public event System.Action OnValueChanged { add { } remove { } }

            public ShipClass ShipClass => ShipClass.Structure;
            public IReadOnlyList<IReadOnlyHardPointModel> HardPointModels => System.Array.Empty<IReadOnlyHardPointModel>();
            public float Hull => IsDestroyed ? 0f : 100f;
            public float HullPercentage => IsDestroyed ? 0f : 1f;
            public float Shields => 0f;
            public float ShieldPercentage => 0f;
            public bool IsDestroyed { get; set; }
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => !IsDestroyed;
            public bool HasLiveHardPoints => false;
            public bool HasShields => false;
            public PlayerId Owner => TestPlayers.Human;

            public IHardPointModel[] GetShipUnits(HardPointType hardPointType) => System.Array.Empty<IHardPointModel>();
        }

        private sealed class OpenEverywhere : IReinforcementSpawnRule
        {
            public bool IsOpen(PlayerId team, Vector3 position) => true;

            public bool CanSpawnShip(PlayerId owner, ShipType shipType, Vector3 position) => true;

            public bool CanLaunchStationDefenseShip(PlayerId owner, ShipType shipType, Vector3 position) =>
                CanSpawnShip(owner, shipType, position);

            public bool IsOpenForStructure(PlayerId owner, Vector3 position) => true;

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

            public bool CanLaunchStationDefenseShip(PlayerId owner, ShipType shipType, Vector3 position) =>
                CanSpawnShip(owner, shipType, position);

            public bool IsOpenForStructure(PlayerId owner, Vector3 position) => IsOpen(owner, position);

            public bool CanSpawnStructure(PlayerId owner, Vector3 position) => IsOpen(owner, position);
        }

        private sealed class BlockedNear : IReinforcementSpawnRule
        {
            private readonly Vector3 _center;
            private readonly float _blockedRadius;

            public BlockedNear(Vector3 center, float blockedRadius)
            {
                _center = center;
                _blockedRadius = blockedRadius;
            }

            public bool IsOpen(PlayerId team, Vector3 position) => PlanarDistance(position, _center) > _blockedRadius;
            public bool CanSpawnShip(PlayerId owner, ShipType shipType, Vector3 position) => IsOpen(owner, position);
            public bool CanLaunchStationDefenseShip(PlayerId owner, ShipType shipType, Vector3 position) => IsOpen(owner, position);
            public bool IsOpenForStructure(PlayerId owner, Vector3 position) => IsOpen(owner, position);
            public bool CanSpawnStructure(PlayerId owner, Vector3 position) => IsOpen(owner, position);
        }
    }
}
