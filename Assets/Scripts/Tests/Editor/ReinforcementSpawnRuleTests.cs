using System;
using System.Collections.Generic;
using System.Reflection;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.Reinforcement;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Services.ShipSpawning;
using EmpireAtWar.Services.SpawnBlocking;
using EmpireAtWar.Services.Vision;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class ReinforcementSpawnRuleTests
    {
        private const float MAP_HALF_SIZE = 1000f;

        private GameObject _root;
        private VisionService _vision;
        private SpawnBlockerService _blockers;
        private FixedClearance _clearance;
        private FixedStructureClearance _structureClearance;
        private FixedRelays _relays;
        private FixedSites _sites;
        private ReinforcementSpawnRule _rule;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject(nameof(ReinforcementSpawnRuleTests));
            PlayerRoster roster = TestPlayers.CreateTeamGame();
            _vision = new VisionService(roster);
            _blockers = new SpawnBlockerService(roster);
            _clearance = new FixedClearance();
            _structureClearance = new FixedStructureClearance();
            _relays = new FixedRelays();
            _sites = new FixedSites();
            _rule = new ReinforcementSpawnRule(_vision, _blockers, _clearance, _structureClearance, _relays, _sites,
                new FixedMap());
            _vision.Register(TestPlayers.Human, CreateTransform(Vector3.zero), 500f);
            _vision.Register(TestPlayers.Enemy, CreateTransform(Vector3.zero), 500f);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_root);

        [Test]
        public void IsOpen_VisibleAndUnblocked_True()
        {
            Assert.That(_rule.IsOpen(TestPlayers.Human, new Vector3(100f, 0f, 0f)), Is.True);
        }

        [Test]
        public void IsOpen_NotVisible_False()
        {
            Assert.That(_rule.IsOpen(TestPlayers.Human, new Vector3(600f, 0f, 0f)), Is.False);
        }

        [Test]
        public void IsOpen_OutsideMap_False()
        {
            _vision.Register(TestPlayers.Human, CreateTransform(new Vector3(MAP_HALF_SIZE, 0f, 0f)), 500f);

            Assert.That(_rule.IsOpen(TestPlayers.Human, new Vector3(MAP_HALF_SIZE + 10f, 0f, 0f)), Is.False);
        }

        [Test]
        public void IsOpen_SameRuleForEveryTeam()
        {
            _blockers.Register(TestPlayers.Enemy, CreateTransform(Vector3.zero), 200f);

            Assert.That(_rule.IsOpen(TestPlayers.Human, new Vector3(100f, 0f, 0f)), Is.False);
            Assert.That(_rule.IsOpen(TestPlayers.Enemy, new Vector3(100f, 0f, 0f)), Is.True);
            Assert.That(_rule.IsOpen(TestPlayers.Human, new Vector3(300f, 0f, 0f)), Is.True);
        }

        [Test]
        public void IsOpen_NeutralAsteroid_BlocksEveryone()
        {
            _blockers.Register(PlayerId.None, CreateTransform(Vector3.zero), 200f);

            Assert.That(_rule.IsOpen(TestPlayers.Human, Vector3.zero), Is.False);
            Assert.That(_rule.IsOpen(TestPlayers.Enemy, Vector3.zero), Is.False);
        }

        [Test]
        public void CanSpawnShip_RequiresClearHull()
        {
            _clearance.IsClearResult = false;

            Assert.That(_rule.CanSpawnShip(TestPlayers.Human, ShipType.Arquitens, Vector3.zero), Is.False);

            _clearance.IsClearResult = true;

            Assert.That(_rule.CanSpawnShip(TestPlayers.Human, ShipType.Arquitens, Vector3.zero), Is.True);
        }

        [Test]
        public void CanSpawnStructure_OpenAndClear_True()
        {
            Assert.That(_rule.CanSpawnStructure(TestPlayers.Human, Vector3.zero), Is.True);
        }

        [Test]
        public void CanSpawnStructure_BlockedPhysically_False()
        {
            _structureClearance.IsClearResult = false;

            Assert.That(_rule.CanSpawnStructure(TestPlayers.Human, Vector3.zero), Is.False);
        }

        [Test]
        public void CanSpawnStructure_InsideRelayRingOrSite_False()
        {
            _relays.Contains = true;

            Assert.That(_rule.CanSpawnStructure(TestPlayers.Human, Vector3.zero), Is.False);

            _relays.Contains = false;
            _sites.Contains = true;

            Assert.That(_rule.CanSpawnStructure(TestPlayers.Human, Vector3.zero), Is.False);
        }

        [Test]
        public void CanSpawnStructure_HostileBlocker_False()
        {
            _blockers.Register(TestPlayers.Enemy, CreateTransform(Vector3.zero), 200f);

            Assert.That(_rule.CanSpawnStructure(TestPlayers.Human, Vector3.zero), Is.False);
        }

        private Transform CreateTransform(Vector3 position)
        {
            Transform transform = new GameObject("Source").transform;
            transform.SetParent(_root.transform);
            transform.position = position;
            return transform;
        }

        private sealed class FixedClearance : IShipSpawnClearance
        {
            public bool IsClearResult { get; set; } = true;

            public float GetPlanarRadius(ShipType shipType) => 1f;

            public bool IsClear(PlayerId owner, ShipType shipType, Vector3 position) => IsClearResult;

            public void ReserveLanding(object ship, PlayerId owner, ShipType shipType, Vector3 position)
            {
            }

            public void ReleaseLanding(object ship)
            {
            }
        }

        private sealed class FixedStructureClearance : IStructureSpawnClearance
        {
            public bool IsClearResult { get; set; } = true;

            public float Radius => 10f;

            public bool IsClear(Vector3 position) => IsClearResult;
        }

        private sealed class FixedRelays : IReinforcementZonesSystem
        {
            public bool Contains { get; set; }

            public event Action OwnershipChanged { add { } remove { } }

            public bool IsPositionInAnyZone(Vector3 position, float clearance = 0f) => Contains;

            public void CopyOwnedCapturableZoneBounds(PlayerId owner, List<Bounds> destination) => destination.Clear();

            public int GetOwnedCapturableZoneCount(PlayerId owner) => 0;

            public bool TryGetDefaultZoneCenter(PlayerId owner, out Vector3 position)
            {
                position = default;
                return false;
            }

            public bool TryGetDefaultZoneExitPosition(PlayerId owner, Vector3 shipPosition, float shipRadius,
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

        private sealed class FixedSites : ICaptureSitesSystem
        {
            public bool Contains { get; set; }

            public IReadOnlyList<ICaptureSite> Sites => Array.Empty<ICaptureSite>();

            public bool IsPositionInAnySite(Vector3 position, float clearance = 0f) => Contains;

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

        private sealed class FixedMap : IMapModelObserver
        {
            public Vector2Range SizeRange { get; } = new Vector2Range();

            public FixedMap()
            {
                SetRangeValue("<Min>k__BackingField", new Vector2(-MAP_HALF_SIZE, -MAP_HALF_SIZE));
                SetRangeValue("<Max>k__BackingField", new Vector2(MAP_HALF_SIZE, MAP_HALF_SIZE));
            }

            public Vector3 GetStationPosition(PlayerId owner) => Vector3.zero;

            private void SetRangeValue(string fieldName, Vector2 value)
            {
                FieldInfo field = typeof(Vector2Range).BaseType?.GetField(
                    fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(field, Is.Not.Null);
                field.SetValue(SizeRange, value);
            }
        }
    }
}
