using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.Layer;
using EmpireAtWar.Services.ReinforcementZones;
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
        private EnemyFactionData _faction;
        private MapStub _map;
        private ZonesStub _zones;
        private EnemyStructurePlacementService _service;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject(nameof(EnemyStructurePlacementServiceTests));
            _faction = ScriptableObject.CreateInstance<EnemyFactionData>();
            _map = new MapStub();
            _zones = new ZonesStub();
            DiContainer container = new DiContainer();
            container.Bind<IMapModelObserver>().FromInstance(_map);
            _service = new EnemyStructurePlacementService(_faction,
                new LazyInject<IMapModelObserver>(container,
                    new InjectContext(container, typeof(IMapModelObserver))),
                _zones, new LayersStub());
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_root);
            UnityEngine.Object.DestroyImmediate(_faction);
        }

        [Test]
        public void ClearStation_IsPreferredOverCapturedZone()
        {
            _zones.Centers.Add(new Vector3(-65f, 0f, 45f));

            Assert.That(_service.TryGetPosition(out Vector3 position), Is.True);

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

            Assert.That(_service.TryGetPosition(out Vector3 position), Is.True);

            Assert.That(Vector3.Distance(position, capturedZone), Is.LessThanOrEqualTo(88f));
            Assert.That(Vector3.Distance(position, _map.Station), Is.GreaterThan(88f));
        }

        [Test]
        public void OccupiedStationWithoutCapturedZone_HasNoMapWideFallback()
        {
            Block(_map.Station, new Vector3(210f, 40f, 210f));

            Assert.That(_service.TryGetPosition(out _), Is.False);
        }

        [Test]
        public void InsufficientMapBounds_RejectsStructureFootprint()
        {
            _map.Station = Vector3.zero;
            _map.SetBounds(-10f, 10f);

            Assert.That(_service.TryGetPosition(out _), Is.False);
        }

        [Test]
        public void DestroyedSite_IsNotReusedWhenItsStructureBecomesDead()
        {
            Assert.That(_service.TryGetPosition(out Vector3 first), Is.True);
            GameObject structure = Block(first, Vector3.one * 20f);

            Assert.That(_service.TryGetPosition(out Vector3 second), Is.True);
            Assert.That(Vector3.Distance(first, second), Is.GreaterThan(16f));

            structure.layer = DEAD_LAYER;
            _service.RecordDestroyedPosition(first);

            Assert.That(_service.TryGetPosition(out Vector3 replacement), Is.True);
            Assert.That(Vector3.Distance(replacement, first), Is.GreaterThan(1f));
        }

        [Test]
        public void Query_WithoutRecordingPlacementDoesNotReservePosition()
        {
            Assert.That(_service.TryGetPosition(out Vector3 first), Is.True);
            Assert.That(_service.TryGetPosition(out Vector3 second), Is.True);

            Assert.That(second, Is.EqualTo(first));
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

            public Vector3 GetStationPosition(FactionType factionType) => Station;
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
            public List<Vector3> Centers { get; } = new List<Vector3>();
            public event Action OwnershipChanged { add { } remove { } }
            public bool IsPositionInAnyZone(Vector3 position, float clearance = 0f) => false;
            public bool IsPositionInOwnedZone(PlayerType playerType, Vector3 position) => false;
            public int GetOwnedCapturableZoneCount(PlayerType playerType) => Centers.Count;
            public bool IsShipSpawnPositionClear(ShipType shipType, Vector3 position) => true;

            public void CopyOwnedCapturableZoneCenters(PlayerType playerType, List<Vector3> destination)
            {
                destination.Clear();
                destination.AddRange(Centers);
            }

            public bool TryGetDefaultSpawnPosition(PlayerType playerType, out Vector3 position)
            {
                position = default;
                return false;
            }

            public bool TryGetDefaultZoneExitPosition(
                PlayerType playerType,
                Vector3 shipPosition,
                float shipRadius,
                out Vector3 position)
            {
                position = default;
                return false;
            }

            public bool TryGetRandomSpawnPosition(PlayerType playerType, ShipType shipType, out Vector3 position)
            {
                position = default;
                return false;
            }

            public bool TryGetCaptureTarget(PlayerType playerType, Vector3 origin, out Vector3 position)
            {
                position = default;
                return false;
            }
        }
    }
}
