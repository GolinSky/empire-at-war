using System;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.SuperWeapons;
using UnityEditor;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.Layer;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Reinforcement;
using EmpireAtWar.Services.ShipSpawning;
using EmpireAtWar.Services.SpawnBlocking;
using EmpireAtWar.Services.Vision;
using EmpireAtWar.Services.Stations;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.ReinforcementZones;
using EmpireAtWar.Presenters.ReinforcementZones;
using EmpireAtWar.Services.Repair;
using EmpireAtWar.Services.Saves;
using EmpireAtWar.Views.ReinforcementZones;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("EnemyAi")]
    public sealed class EnemyStructurePlacementServiceTests
    {
        private const int OBSTACLE_LAYER = 9;
        private const int DEAD_LAYER = 10;

        private GameObject _root;
        private readonly List<RepairProfile> _repairProfiles = new List<RepairProfile>();
        private MapStub _map;
        private ZonesStub _zones;
        private BattleAreaService _areas;
        private EnemyStructurePlacementService _enemyStructurePlacementService;
        private BoxCollider _stationPrefab;
        private BoxCollider _structurePrefab;
        private readonly RecoveryPolicyStub _recovery = new RecoveryPolicyStub();
        private const float HELD_ZONE_RADIUS = 10f;
        private ZoneSourceStub _zoneSource;
        private BaseAccessStub _baseAccess;
        private EntityLocator _entities;
        private PlayerRoster _zoneRoster;
        private long _nextEntityId;

        [SetUp]
        public void SetUp()
        {
            _recovery.IsActive = false;
            _recovery.HasAnchor = false;
            _recovery.Safe = true;
            _recovery.Threat = null;
            new BattleClock(null);
            _root =new GameObject(nameof(EnemyStructurePlacementServiceTests));
            _map = new MapStub();
            _zones = new ZonesStub();
            _zoneSource = new ZoneSourceStub();
            _entities = new EntityLocator();
            _zoneRoster = TestPlayers.CreateDuel();
            _nextEntityId = 1;
            Transform home = new GameObject("Home station").transform;
            home.SetParent(_root.transform);
            _baseAccess = new BaseAccessStub(_map, new BaseEntityStub(_nextEntityId++, home));
            _stationPrefab = CreatePrefab(Vector3.one * 10f);
            _structurePrefab = CreatePrefab(Vector3.one * (32f / Mathf.Sqrt(3f)));
            _enemyStructurePlacementService = CreateService();
        }

        private EnemyStructurePlacementService CreateService()
        {
            PlayerRoster roster = TestPlayers.CreateDuel();
            // The enemy sees the whole test map, so only clearance, relays and bounds decide placement.
            VisionService vision = new VisionService(roster);
            vision.Register(TestPlayers.Enemy, _root.transform, 100000f);
            StructureSpawnClearance structureClearance =
                new StructureSpawnClearance(new LayersStub(), new[] { _structurePrefab });
            _areas = new BattleAreaService(roster);
            ReinforcementSpawnRule spawnRule = new ReinforcementSpawnRule(vision, _areas,
                new OpenShipClearance(), new OpenShipReachability(), structureClearance, _map, new TestHazards());
            return new EnemyStructurePlacementService(
                mapModel: _map,
                zones: _zones, spawnRule: spawnRule, structureClearance: structureClearance,
                vision: vision, areas: _areas,
                owner: roster.Get(TestPlayers.Enemy), stationPrefab: _stationPrefab,
                productionAccess: _baseAccess, recovery: _recovery, zoneSource: _zoneSource,
                world: new AiWorldSnapshot(_entities),
                factionModel: new EnemyFactionModel(null, CreateRoster()), relations: roster);
        }

        // The Separatist roster buys XQ6 platforms; satellites and battle asteroids are capture-site facilities.
        private static FactionRoster CreateRoster()
        {
            const string shared = "Assets/Settings/Data/Factions/Shared/";
            return new FactionRoster(
                AssetDatabase.LoadAssetAtPath<FactionDefinition>(
                    "Assets/Settings/Data/Factions/Separatist/SeparatistFaction.asset"),
                AssetDatabase.LoadAssetAtPath<MiningFacilityCatalog>(shared + "MiningFacilityCatalog.asset"),
                AssetDatabase.LoadAssetAtPath<DefendPlatformCatalog>(shared + "DefendPlatformCatalog.asset"),
                AssetDatabase.LoadAssetAtPath<SuperWeaponCatalog>(shared + "SuperWeaponCatalog.asset"));
        }

        [Test]
        public void RecoveryWithoutAnchor_DefersInsteadOfBuildingAtOldHome()
        {
            _recovery.IsActive = true;
            _recovery.HasAnchor = false;
            Assert.That(_enemyStructurePlacementService.TryGetPosition(out _), Is.False);
        }

        [Test]
        public void RecoveryAnchor_IsUsedInsteadOfOldHome()
        {
            _recovery.IsActive = true;
            _recovery.HasAnchor = true;
            _recovery.Anchor = new Vector3(-100f, 0f, -100f);
            Assert.That(_enemyStructurePlacementService.TryGetPosition(out Vector3 position), Is.True);
            Assert.That(Vector3.Distance(position, _recovery.Anchor), Is.LessThanOrEqualTo(88f));
        }

        [Test]
        public void HostileReach_RejectsOtherwiseOpenPlacement()
        {
            _recovery.Safe = false;
            Assert.That(_enemyStructurePlacementService.TryGetPosition(out _), Is.False);
        }

        private sealed class RecoveryPolicyStub : IEnemyRecoveryPolicy
        {
            public bool IsActive { get; set; }
            public bool HasAnchor { get; set; }
            public bool Safe { get; set; } = true;
            public Vector3 Anchor { get; set; }
            public Vector3? Threat { get; set; }
            public string DeferredReason => "noAnchor";
            public bool TryGetBuildAnchor(out Vector3 position) { position = Anchor; return HasAnchor; }
            public bool IsPlacementSafe(Vector3 position, float clearance) => Safe;
            public void FillPlacementThreats(PlacementThreatSnapshot snapshot)
            {
                snapshot.Clear();
                if (!Safe) snapshot.Add(0f, 0f, 100000f);
                if (Threat is Vector3 threat) snapshot.Add(threat.x, threat.z, 50f);
            }
            public float GetNearestHostileDistance(Vector3 position) => float.PositiveInfinity;
            public void CopySafeAlliedZoneBounds(List<Bounds> bounds) => bounds.Clear();
            public void OnStationBuilt(Vector3 position) { }
        }

        private BoxCollider CreatePrefab(Vector3 size)
        {
            GameObject prefab = new GameObject("Placement prefab");
            prefab.transform.SetParent(_root.transform);
            prefab.SetActive(false);
            BoxCollider collider = prefab.AddComponent<BoxCollider>();
            collider.size = size;
            return collider;
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_root);
            foreach (RepairProfile profile in _repairProfiles) UnityEngine.Object.DestroyImmediate(profile);
            _repairProfiles.Clear();
            new BattleClock(null);
        }

        [Test]
        public void ClearStation_IsPreferredOverCapturedZone()
        {
            AddCapturedZone(new Vector3(-65f, 0f, 45f));

            Assert.That(_enemyStructurePlacementService.TryGetPosition(out Vector3 position), Is.True);

            Assert.That(Vector3.Distance(position, _map.Station), Is.LessThanOrEqualTo(88f));
            Assert.That(Vector3.Distance(position, _map.Station), Is.GreaterThan(45f + 16f));
            Assert.That(Mathf.Abs(position.x) + 16f, Is.LessThanOrEqualTo(250f));
            Assert.That(Mathf.Abs(position.z) + 16f, Is.LessThanOrEqualTo(250f));
        }

        [Test]
        public void OccupiedStation_UsesCapturedZone()
        {
            Block(_map.Station, new Vector3(210f, 40f, 210f));
            Vector3 capturedZone = new Vector3(-65f, 0f, 45f);
            AddCapturedZone(capturedZone);

            Assert.That(_enemyStructurePlacementService.TryGetPosition(out Vector3 position), Is.True);

            Assert.That(Vector3.Distance(position, capturedZone), Is.LessThanOrEqualTo(88f));
            Assert.That(Vector3.Distance(position, _map.Station), Is.GreaterThan(88f));
        }

        [Test]
        public void OccupiedStationWithoutCapturedZone_UsesNearestVisibleSpotFurtherOut()
        {
            Block(_map.Station, new Vector3(210f, 40f, 210f));

            Assert.That(_enemyStructurePlacementService.TryGetPosition(out Vector3 position), Is.True);

            // Outside the 210-unit block centered on the station.
            Assert.That(Mathf.Max(Mathf.Abs(position.x - _map.Station.x), Mathf.Abs(position.z - _map.Station.z)),
                Is.GreaterThan(105f));
        }

        [Test]
        public void FullySeenMap_HasNothingToScout()
        {
            Assert.That(_enemyStructurePlacementService.TryGetScoutTarget(out _), Is.False);
        }

        [Test]
        public void InsufficientMapBounds_RejectsStructureFootprint()
        {
            _map.Station = Vector3.zero;
            _map.SetBounds(-10f, 10f);

            Assert.That(_enemyStructurePlacementService.TryGetPosition(out _), Is.False);
        }

        [Test]
        public void DestroyedSite_IsNotReusedWhenItsStructureBecomesDead()
        {
            Assert.That(_enemyStructurePlacementService.TryGetPosition(out Vector3 first), Is.True);
            GameObject structure = Block(first, Vector3.one * 20f);

            Assert.That(_enemyStructurePlacementService.TryGetPosition(out Vector3 second), Is.True);
            Assert.That(Vector3.Distance(first, second), Is.GreaterThan(16f));

            structure.layer = DEAD_LAYER;
            _enemyStructurePlacementService.RecordDestroyedPosition(first);

            Assert.That(_enemyStructurePlacementService.TryGetPosition(out Vector3 replacement), Is.True);
            Assert.That(Vector3.Distance(replacement, first), Is.GreaterThanOrEqualTo(32f));
        }

        [Test]
        public void Query_WithoutRecordingPlacementDoesNotReservePosition()
        {
            Assert.That(_enemyStructurePlacementService.TryGetPosition(out Vector3 first), Is.True);
            Assert.That(_enemyStructurePlacementService.TryGetPosition(out Vector3 second), Is.True);

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void PreviouslyAcceptedPosition_IsRevalidatedAndReusedWhileStillClear()
        {
            Assert.That(_enemyStructurePlacementService.TryGetPosition(out Vector3 first), Is.True);
            GameObject obstacle = Block(first, Vector3.one * 20f);
            Assert.That(_enemyStructurePlacementService.TryGetPosition(out Vector3 alternate), Is.True);
            Assert.That(alternate, Is.Not.EqualTo(first));
            UnityEngine.Object.DestroyImmediate(obstacle);

            Assert.That(_enemyStructurePlacementService.TryGetPosition(out Vector3 reused), Is.True);
            Assert.That(reused, Is.EqualTo(alternate));
        }

        [Test]
        public void ScaledOffsetStation_SearchesOutsideItsFootprint()
        {
            _map.Station = Vector3.zero;
            _map.SetBounds(-1000f, 1000f);
            _stationPrefab.center = new Vector3(40f, 15f, -10f);
            _stationPrefab.size = new Vector3(100f, 30f, 80f);
            _stationPrefab.transform.localScale = Vector3.one * 3f;
            _stationPrefab.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            _enemyStructurePlacementService = CreateService();

            Assert.That(_enemyStructurePlacementService.TryGetPosition(out Vector3 position), Is.True);

            Vector3 center = _stationPrefab.transform.TransformVector(_stationPrefab.center);
            center.y = 0f;
            float radius = new Vector2(150f, 45f).magnitude;
            float secondStationRadius = new Vector3(270f, 150f, 90f).magnitude;
            Assert.That(Vector3.Distance(position, center), Is.GreaterThan(radius + secondStationRadius));
            Assert.That(Mathf.Abs(position.x) + secondStationRadius, Is.LessThanOrEqualTo(1000f));
            Assert.That(Mathf.Abs(position.z) + secondStationRadius, Is.LessThanOrEqualTo(1000f));
        }

        [Test]
        public void ScaledOffsetStructure_RejectsOverlapBeyondLegacyClearance()
        {
            _map.Station = Vector3.zero;
            _map.SetBounds(-1000f, 1000f);
            _structurePrefab.transform.localScale = Vector3.one * 3f;
            _structurePrefab.center = new Vector3(3f, 0f, 2f);
            _enemyStructurePlacementService = CreateService();
            Assert.That(_enemyStructurePlacementService.TryGetPosition(out Vector3 first), Is.True);
            Vector3 obstaclePosition = first + Vector3.right * 35f;
            Block(obstaclePosition, Vector3.one * 4f);

            Assert.That(_enemyStructurePlacementService.TryGetPosition(out Vector3 next), Is.True);

            Assert.That(next, Is.Not.EqualTo(first));
            Assert.That(Vector3.Distance(next, obstaclePosition), Is.GreaterThan(48f));
        }

        [Test]
        public void CapturedZone_UsesItsActualRadius()
        {
            _map.Station = new Vector3(600f, 0f, 600f);
            _map.SetBounds(-1000f, 1000f);
            Block(_map.Station, new Vector3(210f, 40f, 210f));
            _zones.Radius = 270f;
            AddCapturedZone(Vector3.zero);

            Assert.That(_enemyStructurePlacementService.TryGetPosition(out Vector3 position), Is.True);

            Assert.That(position.magnitude, Is.GreaterThan(270f + 16f));
            Assert.That(position.magnitude, Is.LessThanOrEqualTo(270f + 16f + 36f));
        }

        [Test]
        public void DefensePosition_ZoneWithAPlatformOnItsRingIsSkippedForTheNextHeldZone()
        {
            _map.SetBounds(-1000f, 1000f);
            AddZone(TestPlayers.Enemy, Vector3.zero, isMain: true);
            Vector3 relay = new Vector3(400f, 0f, 0f);
            AddZone(TestPlayers.Enemy, relay);

            SettleHeldZones();
            Assert.That(_enemyStructurePlacementService.TryGetDefensePosition(false, out Vector3 first), Is.True);
            // The platform ring lies outside the doubled zone radius, where the zone itself ends.
            Assert.That(first.magnitude, Is.GreaterThan(2f * HELD_ZONE_RADIUS));
            AddPlatform(first);

            Assert.That(_enemyStructurePlacementService.TryGetDefensePosition(false, out Vector3 next), Is.True);
            Assert.That(Vector3.Distance(next, relay), Is.LessThan(next.magnitude));
        }

        [Test]
        public void DefensePosition_EveryHeldZoneDefended_OnlyAnExtraRequestFindsAPosition()
        {
            _map.SetBounds(-1000f, 1000f);
            AddZone(TestPlayers.Enemy, Vector3.zero, isMain: true);
            Vector3 relay = new Vector3(400f, 0f, 0f);
            AddZone(TestPlayers.Enemy, relay);
            // The centre takes two platforms, every other held zone one.
            AddPlatform(new Vector3(70f, 0f, 0f));
            AddPlatform(new Vector3(-70f, 0f, 0f));
            AddPlatform(relay + new Vector3(70f, 0f, 0f));

            SettleHeldZones();
            Assert.That(_enemyStructurePlacementService.TryGetDefensePosition(false, out _), Is.False);
            Assert.That(_enemyStructurePlacementService.TryGetDefensePosition(true, out Vector3 extra), Is.True);
            // The relay has fewer platforms, so its extra comes first.
            Assert.That(Vector3.Distance(extra, relay), Is.LessThan(extra.magnitude));
        }

        [Test]
        public void DefensePosition_FirstOnly_ZoneWithAPlatformGetsNoSecond()
        {
            _map.SetBounds(-1000f, 1000f);
            AddZone(TestPlayers.Enemy, Vector3.zero, isMain: true);
            AddPlatform(new Vector3(70f, 0f, 0f));

            SettleHeldZones();
            Assert.That(_enemyStructurePlacementService.TryGetDefensePosition(false, out _), Is.True,
                "The centre takes a second platform when ships do not compete.");
            Assert.That(_enemyStructurePlacementService.TryGetDefensePosition(false, true, out _), Is.False,
                "While a ship can be bought only first platforms are placed.");
        }

        [Test]
        public void RepairPlatformPosition_NeedsAnOwnArmedPlatform()
        {
            _map.SetBounds(-1000f, 1000f);
            Assert.That(_enemyStructurePlacementService.TryGetRepairPlatformPosition(out _), Is.False);
        }

        [Test]
        public void RepairPlatformPosition_StandsBesideAnArmedPlatformUntilARepairerCoversIt()
        {
            _map.SetBounds(-1000f, 1000f);
            Vector3 armed = new Vector3(400f, 0f, 0f);
            AddPlatform(armed);

            Assert.That(_enemyStructurePlacementService.TryGetRepairPlatformPosition(out Vector3 position), Is.True);
            RepairProfile profile = AddRepairPlatform(position);
            Assert.That(Vector3.Distance(position, armed), Is.LessThanOrEqualTo(profile.Radius),
                "The repair platform covers the armed platform it joins.");
            Assert.That(_enemyStructurePlacementService.TryGetRepairPlatformPosition(out _), Is.False,
                "A covered platform needs no second repairer.");
        }

        [Test]
        public void DefensePosition_ARepairPlatformDoesNotTakeAnArmedPlatformSlot()
        {
            _map.SetBounds(-1000f, 1000f);
            AddZone(TestPlayers.Enemy, Vector3.zero, isMain: true);
            AddPlatform(new Vector3(70f, 0f, 0f));
            AddRepairPlatform(new Vector3(-70f, 0f, 0f));

            SettleHeldZones();
            Assert.That(_enemyStructurePlacementService.TryGetDefensePosition(false, out _), Is.True,
                "The centre still takes its second armed platform.");
        }

        [Test]
        public void DefensePosition_SatellitesAndBattleAsteroidsAreNotPlatforms()
        {
            _map.SetBounds(-1000f, 1000f);
            Vector3 relay = new Vector3(400f, 0f, 0f);
            AddZone(TestPlayers.Enemy, relay);
            AddPlatform(relay + new Vector3(70f, 0f, 0f), DefendPlatformType.QuadLaserSatellite);
            AddPlatform(relay + new Vector3(-70f, 0f, 0f), DefendPlatformType.BattleAsteroid);

            SettleHeldZones();
            Assert.That(_enemyStructurePlacementService.TryGetDefensePosition(false, out _), Is.True);
            AddPlatform(relay + new Vector3(0f, 0f, 70f));
            Assert.That(_enemyStructurePlacementService.TryGetDefensePosition(false, out _), Is.False);
        }

        [Test]
        public void SecondStation_LandsOnTheHomeRingOnTheCornerSide()
        {
            _map.SetBounds(-1500f, 1500f);
            _map.Station = new Vector3(800f, 0f, -800f);

            Assert.That(_enemyStructurePlacementService.TryGetSecondStationPosition(out Vector3 position), Is.True);
            Vector3 fromStation = position - _map.Station;
            fromStation.y = 0f;
            Assert.That(fromStation.magnitude, Is.InRange(380f, 720f));
            Assert.That(Vector3.Dot(fromStation, new Vector3(_map.Station.x, 0f, _map.Station.z)), Is.GreaterThan(0f),
                "The second station stands on the half away from the map centre.");
        }

        [Test]
        public void DefensePosition_ExtraPlatformsSpreadEvenlyAndStopAtTheZoneLimit()
        {
            _map.SetBounds(-1000f, 1000f);
            AddZone(TestPlayers.Enemy, Vector3.zero, isMain: true);
            Vector3 relay = new Vector3(400f, 0f, 0f);
            AddZone(TestPlayers.Enemy, relay);
            int atCentre = 0;
            int atRelay = 0;
            SettleHeldZones();
            while (_enemyStructurePlacementService.TryGetDefensePosition(true, out Vector3 position))
            {
                AddPlatform(position);
                if (position.magnitude < Vector3.Distance(position, relay)) atCentre++;
                else atRelay++;
                // No zone runs ahead of the other by more than one platform.
                Assert.That(Mathf.Abs(atCentre - atRelay), Is.LessThanOrEqualTo(1));
                Assert.That(atCentre + atRelay, Is.LessThanOrEqualTo(20), "Platform placement never stopped.");
            }

            Assert.That(atCentre, Is.EqualTo(atRelay));
            Assert.That(atCentre, Is.GreaterThan(1));
        }

        [Test]
        public void DefensePosition_IgnoresZonesHeldByAnotherPlayer()
        {
            _map.SetBounds(-1000f, 1000f);
            AddZone(TestPlayers.Human, Vector3.zero, isMain: true);

            Assert.That(_enemyStructurePlacementService.TryGetDefensePosition(true, out _), Is.False);
        }

        // W50: zones count once held, uncontested, for the hold time.
        [Test]
        public void DefensePosition_ZoneHeldShorterThanTheHoldTime_GetsNoPlatform()
        {
            _map.SetBounds(-1000f, 1000f);
            AddZone(TestPlayers.Enemy, new Vector3(400f, 0f, 0f));

            Assert.That(_enemyStructurePlacementService.TryGetDefensePosition(false, out _), Is.False,
                "A zone just taken gets no platform.");
            new BattleClock(null).Restore(EnemyStructurePlacementService.PLATFORM_ZONE_HOLD_SECONDS * 0.5f);
            Assert.That(_enemyStructurePlacementService.TryGetDefensePosition(false, out _), Is.False);
            new BattleClock(null).Restore(EnemyStructurePlacementService.PLATFORM_ZONE_HOLD_SECONDS);
            Assert.That(_enemyStructurePlacementService.TryGetDefensePosition(false, out _), Is.True);
        }

        [Test]
        public void DefensePosition_ContestedZone_GetsNoPlatform()
        {
            _map.SetBounds(-1000f, 1000f);
            ReinforcementZone relay = AddZone(TestPlayers.Enemy, new Vector3(400f, 0f, 0f));
            SettleHeldZones();
            Assert.That(_enemyStructurePlacementService.TryGetDefensePosition(false, out _), Is.True);

            relay.Restore(TestPlayers.Enemy, TestPlayers.Human, 0.5f, true);

            Assert.That(_enemyStructurePlacementService.TryGetDefensePosition(false, out _), Is.False);
        }

        [Test]
        public void DefensePosition_RetakenZone_WaitsTheHoldTimeAgain()
        {
            _map.SetBounds(-1000f, 1000f);
            ReinforcementZone relay = AddZone(TestPlayers.Enemy, new Vector3(400f, 0f, 0f));
            SettleHeldZones();
            relay.Restore(TestPlayers.Human, PlayerId.None, 0f, false);
            Assert.That(_enemyStructurePlacementService.TryGetDefensePosition(false, out _), Is.False);

            relay.Restore(TestPlayers.Enemy, PlayerId.None, 0f, false);

            Assert.That(_enemyStructurePlacementService.TryGetDefensePosition(false, out _), Is.False,
                "The hold time counts from the retake.");
        }

        [Test]
        public void DefensePosition_BigPlatform_NeedsKnownHostilesBeyondTheStandOffRange()
        {
            _map.SetBounds(-3000f, 3000f);
            Vector3 relay = new Vector3(400f, 0f, 0f);
            AddZone(TestPlayers.Enemy, relay);
            SettleHeldZones();
            // Far enough for a small platform's safety test, inside the big-platform stand-off range.
            _recovery.Threat = relay + Vector3.forward * (EnemyStructurePlacementService.BIG_PLATFORM_HOSTILE_RANGE * 0.8f);

            Assert.That(_enemyStructurePlacementService.TryGetDefensePosition(false, false, false, out _), Is.True);
            Assert.That(_enemyStructurePlacementService.TryGetDefensePosition(false, false, true, out _), Is.False);

            _recovery.Threat = relay + Vector3.forward * (EnemyStructurePlacementService.BIG_PLATFORM_HOSTILE_RANGE * 1.5f);
            Assert.That(_enemyStructurePlacementService.TryGetDefensePosition(false, false, true, out _), Is.True);
        }

        /// <summary>Lets the service see the held zones now, then moves the battle clock past the hold time.</summary>
        private void SettleHeldZones()
        {
            float now = BattleClock.CurrentElapsed;
            _enemyStructurePlacementService.TryGetDefensePosition(false, out _);
            new BattleClock(null).Restore(now + EnemyStructurePlacementService.PLATFORM_ZONE_HOLD_SECONDS);
        }

        private ReinforcementZone AddZone(PlayerId owner, Vector3 center, bool isMain = false)
        {
            var zone = new ReinforcementZone(new HeldZoneView(center),
                TestPlayers.CreateLocalPlayer(_zoneRoster),
                new ReinforcementZoneModel(_zoneRoster, owner, 10f, 1f, true), isMain);
            _zoneSource.Items.Add(zone);
            return zone;
        }

        private void AddPlatform(Vector3 position, DefendPlatformType type = DefendPlatformType.Xq6)
        {
            Transform platform = new GameObject("Defend platform").transform;
            platform.SetParent(_root.transform);
            platform.position = position;
            _entities.AddEntity(new PlatformEntity(_nextEntityId++, platform, type));
        }

        private RepairProfile AddRepairPlatform(Vector3 position)
        {
            Transform platform = new GameObject("Repair platform").transform;
            platform.SetParent(_root.transform);
            platform.position = position;
            RepairProfile profile = ScriptableObject.CreateInstance<RepairProfile>();
            _repairProfiles.Add(profile);
            _entities.AddEntity(new RepairPlatformEntity(_nextEntityId++, platform, profile));
            return profile;
        }

        private sealed class RepairPlatformEntity : PlatformEntity, IRepairSourceFacade
        {
            public event Action TargetsChanged { add { } remove { } }
            public RepairProfile Profile { get; }
            public Vector3 Position => Transform.position;
            public bool CanRepair => true;
            public IReadOnlyList<IEntity> Targets => Array.Empty<IEntity>();

            public RepairPlatformEntity(long id, Transform transform, RepairProfile profile)
                : base(id, transform, DefendPlatformType.Xq6Repair)
            {
                Profile = profile;
            }

            public void SetTargets(IReadOnlyList<IEntity> targets) { }
        }

        private sealed class ZoneSourceStub : IReinforcementZoneSource
        {
            public List<ReinforcementZone> Items { get; } = new List<ReinforcementZone>();
            public IReadOnlyList<ReinforcementZone> Zones => Items;
            public float SquadronCaptureWeight => 0.5f;
            public (float Friendly, float Hostile) GetTeamStrengths(ReinforcementZone zone, PlayerId owner) => default;
        }

        private sealed class HeldZoneView : IReinforcementZoneComponent
        {
            public Vector3 Center { get; }
            public float Radius => HELD_ZONE_RADIUS;
            public PlayerId StartingOwner => PlayerId.None;
            public bool IsCapturable => true;
            public float CaptureDuration => 10f;
            public HeldZoneView(Vector3 center) { Center = center; }
            public void SetVisibility(bool isVisible, bool showCaptureUi) { }
            public void Render(OwnerRelation owner, OwnerRelation capturer, float captureProgress, bool isContested,
                OwnerRelation contestingFirst = OwnerRelation.Neutral, OwnerRelation contestingSecond = OwnerRelation.Neutral) { }
        }

        private class PlatformEntity : IEntity, IHealthModelObserver, IEntityTransformFacade, IStructureKindFacade,
            IDefendPlatformTypeFacade
        {
            public event Action OnDestroy { add { } remove { } }
            public event Action OnValueChanged { add { } remove { } }

            public long Id { get; }
            public Transform Transform { get; }
            public IHealthModelObserver HealthModel => this;
            public PlayerId Owner => TestPlayers.Enemy;
            public StructureKind Kind => StructureKind.DefendPlatform;
            public ShipClass ShipClass => ShipClass.Structure;
            public IReadOnlyList<IReadOnlyHardPointModel> HardPointModels => Array.Empty<IReadOnlyHardPointModel>();
            public float Hull => 1f;
            public float HullPercentage => 1f;
            public float Shields => 0f;
            public float ShieldPercentage => 0f;
            public bool IsDestroyed => false;
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public bool HasLiveHardPoints => true;
            public bool HasShields => false;

            public DefendPlatformType Type { get; }

            public PlatformEntity(long id, Transform transform, DefendPlatformType type)
            {
                Id = id;
                Transform = transform;
                Type = type;
            }

            public IHardPointModel[] GetShipUnits(HardPointType hardPointType) => Array.Empty<IHardPointModel>();

            public bool TryGetFacade<TFacade>(out TFacade entityFacade) where TFacade : IEntityFacade
            {
                if (this is TFacade facade)
                {
                    entityFacade = facade;
                    return true;
                }
                entityFacade = default;
                return false;
            }

            public TFacade GetFacade<TFacade>() where TFacade : IEntityFacade
            {
                if (TryGetFacade(out TFacade facade)) return facade;
                throw new InvalidOperationException($"Entity {Id} has no {typeof(TFacade).Name}.");
            }
        }

        [Test]
        public void LostMainStation_PlacementFollowsTheLivingSecondStationAndAvoidsTheWreck()
        {
            _map.SetBounds(-1500f, 1500f);
            _map.Station = new Vector3(-600f, 0f, -600f);
            Assert.That(_enemyStructurePlacementService.TryGetPosition(out _), Is.True);
            Transform second = new GameObject("Second station").transform;
            second.SetParent(_root.transform);
            second.position = new Vector3(600f, 0f, 600f);
            _baseAccess.Base.IsDestroyed = true;
            _baseAccess.Base = new BaseEntityStub(_nextEntityId++, second);
            _baseAccess.FollowMap = false;

            Assert.That(_enemyStructurePlacementService.TryGetPosition(out Vector3 position), Is.True);
            Assert.That(Vector3.Distance(position, second.position), Is.LessThan(Vector3.Distance(position, _map.Station)));
            Assert.That(_enemyStructurePlacementService.CanPlaceMiner, Is.False, "The turtle after a base loss pauses miners.");
        }

        [Test]
        public void LostBaseArea_IsClosedToPlacementForTheBattle()
        {
            _map.SetBounds(-1500f, 1500f);
            _map.Station = new Vector3(-600f, 0f, -600f);
            Assert.That(_enemyStructurePlacementService.TryGetPosition(out _), Is.True);
            Transform second = new GameObject("Second station").transform;
            second.SetParent(_root.transform);
            // The new base stands inside the old one's closed area: nothing is placed around it there.
            second.position = _map.Station + new Vector3(150f, 0f, 0f);
            _baseAccess.Base.IsDestroyed = true;
            _baseAccess.Base = new BaseEntityStub(_nextEntityId++, second);
            _baseAccess.FollowMap = false;

            bool found = _enemyStructurePlacementService.TryGetPosition(out Vector3 position);
            Assert.That(found, Is.True, "The map-wide search still finds a point outside the closed area.");
            Assert.That(Vector3.Distance(position, _map.Station), Is.GreaterThan(400f));
        }

        [Test]
        public void NoLivingBase_SearchesNothing()
        {
            _baseAccess.Base.IsDestroyed = true;

            Assert.That(_enemyStructurePlacementService.TryGetPosition(out _), Is.False);
            Assert.That(_enemyStructurePlacementService.TryGetSecondStationPosition(out _), Is.False);
            Assert.That(_enemyStructurePlacementService.CanPlaceMiner, Is.False);
        }

        [Test]
        public void HostileNearTheBase_PausesMiners_AndPlacementFacesAwayFromIt()
        {
            _map.SetBounds(-1500f, 1500f);
            _map.Station = Vector3.zero;
            Assert.That(_enemyStructurePlacementService.CanPlaceMiner, Is.True);
            _recovery.Threat = new Vector3(500f, 0f, 0f);

            Assert.That(_enemyStructurePlacementService.CanPlaceMiner, Is.False);
            Assert.That(_enemyStructurePlacementService.TryGetPosition(out Vector3 position), Is.True);
            Assert.That(position.x, Is.LessThanOrEqualTo(0f), "Miners go on the half of the base away from hostiles.");
        }

        private sealed class BaseAccessStub : IProductionAccess
        {
            private readonly MapStub _map;
            public BaseEntityStub Base { get; set; }
            /// <summary>The base stands at the map's station slot, wherever a test moves it.</summary>
            public bool FollowMap { get; set; } = true;
            public PlayerId ProductionHost => TestPlayers.Enemy;
            public bool CanProduce => !Base.IsDestroyed;
            public bool HasHostBase => CanProduce;

            public BaseAccessStub(MapStub map, BaseEntityStub productionBase)
            {
                _map = map;
                Base = productionBase;
            }

            public bool TryGetProductionBase(out IEntity productionBase)
            {
                productionBase = CanProduce ? Base : null;
                if (CanProduce && FollowMap) Base.Transform.position = _map.Station;
                return CanProduce;
            }
        }

        private sealed class BaseEntityStub : IEntity, IHealthModelObserver, IEntityTransformFacade
        {
            public event Action OnDestroy { add { } remove { } }
            public event Action OnValueChanged { add { } remove { } }

            public long Id { get; }
            public Transform Transform { get; }
            public IHealthModelObserver HealthModel => this;
            public PlayerId Owner => TestPlayers.Enemy;
            public ShipClass ShipClass => ShipClass.Structure;
            public IReadOnlyList<IReadOnlyHardPointModel> HardPointModels => Array.Empty<IReadOnlyHardPointModel>();
            public float Hull => 1f;
            public float HullPercentage => 1f;
            public float Shields => 0f;
            public float ShieldPercentage => 0f;
            public bool IsDestroyed { get; set; }
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => !IsDestroyed;
            public bool HasLiveHardPoints => true;
            public bool HasShields => false;

            public BaseEntityStub(long id, Transform transform)
            {
                Id = id;
                Transform = transform;
            }

            public IHardPointModel[] GetShipUnits(HardPointType hardPointType) => Array.Empty<IHardPointModel>();

            public bool TryGetFacade<TFacade>(out TFacade entityFacade) where TFacade : IEntityFacade
            {
                if (this is TFacade facade)
                {
                    entityFacade = facade;
                    return true;
                }
                entityFacade = default;
                return false;
            }

            public TFacade GetFacade<TFacade>() where TFacade : IEntityFacade
            {
                if (TryGetFacade(out TFacade facade)) return facade;
                throw new InvalidOperationException($"Entity {Id} has no {typeof(TFacade).Name}.");
            }
        }

        private void AddCapturedZone(Vector3 center)
        {
            _zones.Centers.Add(center);
            Transform zone = new GameObject("Captured zone").transform;
            zone.SetParent(_root.transform);
            zone.position = center;
            _areas.Register(BattleAreaLayer.StructureExclusion, TestPlayers.Enemy, zone, _zones.Radius);
        }

        private GameObject Block(Vector3 position, Vector3 size)
        {
            GameObject obstacle = new GameObject("Occupied structure site");
            obstacle.transform.SetParent(_root.transform);
            obstacle.transform.position = position;
            obstacle.layer = OBSTACLE_LAYER;
            obstacle.AddComponent<BoxCollider>().size = size;
            return obstacle;
        }

        private sealed class MapStub : IMapModelObserver
        {
            public Vector3 Station = new Vector3(160f, 0f, -170f);

            public Vector2Range SizeRange { get; } = new Vector2Range();

            public MapStub()
            {
                SetBounds(-250f, 250f);
            }

            public void SetBounds(float minimum, float maximum)
            {
                Type rangeType = typeof(Vector2Range).BaseType;
                rangeType.GetField("<Min>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(SizeRange, Vector2.one * minimum);
                rangeType.GetField("<Max>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(SizeRange, Vector2.one * maximum);
            }

            public Vector3 GetStationPosition(PlayerId owner) => Station;
        }

        private sealed class LayersStub : ILayerService
        {
            public LayerMask CoreGameCameraMask => throw new NotSupportedException();

            public LayerMask CinemaCameraMask => throw new NotSupportedException();

            public LayerMask GetMask(params LayerKey[] keys) => 1 << OBSTACLE_LAYER;

            public int GetLayer(LayerKey key) => throw new NotSupportedException();

            public bool IsInLayer(GameObject gameObject, LayerKey key) => throw new NotSupportedException();

            public void Apply(GameObject gameObject, LayerKey key, bool includeChildren) =>
                throw new NotSupportedException();
        }

        private sealed class ZonesStub : IReinforcementZonesSystem
        {
            public event Action OwnershipChanged { add { } remove { } }

            public List<Vector3> Centers { get; } = new List<Vector3>();
            public float Radius { get; set; }

            public int GetOwnedCapturableZoneCount(PlayerId owner) => Centers.Count;

            public void CopyOwnedCapturableZoneBounds(PlayerId owner, List<Bounds> destination)
            {
                destination.Clear();
                foreach (Vector3 center in Centers)
                {
                    destination.Add(new Bounds(center, new Vector3(Radius * 2f, 0f, Radius * 2f)));
                }
            }

            public bool TryGetDefaultZoneCenter(PlayerId owner, out Vector3 position)
            {
                position = default;
                return false;
            }

            public bool TryGetDefaultZoneExitPosition(
                PlayerId owner,
                Vector3 shipPosition,
                float shipRadius,
                out Vector3 position)
            {
                position = default;
                return false;
            }

            public bool TryGetCaptureTarget(PlayerId owner, Vector3 origin, out Vector3 position)
            {
                position = default;
                return false;
            }
        }

        private sealed class OpenShipReachability : IShipSpawnReachability
        {
            public bool IsReachable(PlayerId owner, ShipType shipType, Vector3 position) => true;

            public void FillReachability(PlayerId owner, ShipType shipType, Vector3[] positions, bool[] reachable) =>
                System.Array.Fill(reachable, true);

            public bool TryFillReachability(PlayerId owner, ShipType shipType, Vector3[] positions,
                bool[] reachable, bool forceRefresh)
            {
                FillReachability(owner, shipType, positions, reachable);
                return true;
            }
        }

        private sealed class OpenShipClearance : IShipSpawnClearance
        {
            public float GetPlanarRadius(ShipType shipType) => 1f;

            public bool IsClear(PlayerId owner, ShipType shipType, Vector3 position) => true;

            public bool IsClear(ShipType shipType, Vector3 position, Quaternion rotation) => true;

            public void ReserveLanding(object ship, PlayerId owner, ShipType shipType, Vector3 position)
            {
            }

            public void ReserveLanding(object ship, ShipType shipType, Vector3 position, Quaternion rotation)
            {
            }

            public void ReleaseLanding(object ship)
            {
            }
        }
    }
}
