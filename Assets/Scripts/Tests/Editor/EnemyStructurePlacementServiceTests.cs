using System;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.Layer;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Reinforcement;
using NUnit.Framework;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class EnemyStructurePlacementServiceTests
    {
        private const int OBSTACLE_LAYER = 9;
        private const int DEAD_LAYER = 10;

        private GameObject _root;
        private MapStub _map;
        private ZonesStub _zones;
        private EnemyStructurePlacementService _enemyStructurePlacementService;
        private BoxCollider _stationPrefab;
        private BoxCollider _structurePrefab;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject(nameof(EnemyStructurePlacementServiceTests));
            _map = new MapStub();
            _zones = new ZonesStub();
            _stationPrefab = CreatePrefab(Vector3.one * 10f);
            _structurePrefab = CreatePrefab(Vector3.one * (32f / Mathf.Sqrt(3f)));
            _enemyStructurePlacementService = CreateService();
        }

        private EnemyStructurePlacementService CreateService()
        {
            DiContainer container = new DiContainer();
            container.Bind<IMapModelObserver>().FromInstance(_map);
            return new EnemyStructurePlacementService(
                mapModel: new LazyInject<IMapModelObserver>(container,
                    new InjectContext(container, typeof(IMapModelObserver))),
                zones: _zones, captureSites: new NoCaptureSites(), spawnRule: new AllOpen(), layerService: new LayersStub(), owner: TestPlayers.CreateDuel().Get(TestPlayers.Enemy),
                stationPrefab: _stationPrefab, structurePrefabs: new[] { _structurePrefab });
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
        }

        [Test]
        public void ClearStation_IsPreferredOverCapturedZone()
        {
            _zones.Centers.Add(new Vector3(-65f, 0f, 45f));

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
            _zones.Centers.Add(capturedZone);

            Assert.That(_enemyStructurePlacementService.TryGetPosition(out Vector3 position), Is.True);

            Assert.That(Vector3.Distance(position, capturedZone), Is.LessThanOrEqualTo(88f));
            Assert.That(Vector3.Distance(position, _map.Station), Is.GreaterThan(88f));
        }

        [Test]
        public void OccupiedStationWithoutCapturedZone_HasNoMapWideFallback()
        {
            Block(_map.Station, new Vector3(210f, 40f, 210f));

            Assert.That(_enemyStructurePlacementService.TryGetPosition(out _), Is.False);
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
            Assert.That(Vector3.Distance(replacement, first), Is.GreaterThan(1f));
        }

        [Test]
        public void Query_WithoutRecordingPlacementDoesNotReservePosition()
        {
            Assert.That(_enemyStructurePlacementService.TryGetPosition(out Vector3 first), Is.True);
            Assert.That(_enemyStructurePlacementService.TryGetPosition(out Vector3 second), Is.True);

            Assert.That(second, Is.EqualTo(first));
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
            Assert.That(Vector3.Distance(position, center), Is.GreaterThan(radius + 16f));
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
            _zones.Centers.Add(Vector3.zero);

            Assert.That(_enemyStructurePlacementService.TryGetPosition(out Vector3 position), Is.True);

            Assert.That(position.magnitude, Is.GreaterThan(270f + 16f));
            Assert.That(position.magnitude, Is.LessThanOrEqualTo(270f + 16f + 36f));
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

            public bool IsPositionInAnyZone(Vector3 position, float clearance = 0f)
            {
                return Centers.Exists(center => Vector3.Distance(center, position) <= Radius + clearance);
            }

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

        private sealed class AllOpen : IReinforcementSpawnRule
        {
            public bool IsOpen(PlayerId team, Vector3 position) => true;

            public bool CanSpawnShip(PlayerId owner, ShipType shipType, Vector3 position) => true;
        }

        private sealed class NoCaptureSites : ICaptureSitesSystem
        {
            public IReadOnlyList<ICaptureSite> Sites =>
                Array.Empty<ICaptureSite>();

            public bool IsPositionInAnySite(Vector3 position, float clearance = 0f) => false;

            public bool TryGetCaptureTarget(PlayerId owner, Vector3 origin, out Vector3 position)
            {
                position = default;
                return false;
            }

            public bool TryBuildOnOwnedSite(PlayerId owner) => false;

            public bool TryGetThreatenedSite(PlayerId owner, out Vector3 position)
            {
                position = default;
                return false;
            }

            public bool TryGetRaidTarget(PlayerId attacker, Vector3 origin, out Vector3 position)
            {
                position = default;
                return false;
            }
        }
    }
}
