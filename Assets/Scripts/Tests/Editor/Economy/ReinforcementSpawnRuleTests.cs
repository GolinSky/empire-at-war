using System.Reflection;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.SkirmishCamera;
using EmpireAtWar.Services.Reinforcement;
using EmpireAtWar.Services.ShipSpawning;
using EmpireAtWar.Services.SpawnBlocking;
using EmpireAtWar.Services.Vision;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.Tests.Editor
{
    [Category("Economy")]
    public sealed class ReinforcementSpawnRuleTests
    {
        private const float MAP_HALF_SIZE = 1000f;

        private GameObject _root;
        private VisionService _vision;
        private BattleAreaService _blockers;
        private FixedClearance _clearance;
        private FixedReachability _reachability;
        private FixedStructureClearance _structureClearance;
        private ReinforcementSpawnRule _rule;
        private TestHazards _hazards;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject(nameof(ReinforcementSpawnRuleTests));
            PlayerRoster roster = TestPlayers.CreateTeamGame();
            _vision = new VisionService(roster);
            _blockers = new BattleAreaService(roster);
            _clearance = new FixedClearance();
            _structureClearance = new FixedStructureClearance();
            _hazards = new TestHazards();
            _reachability = new FixedReachability();
            _rule = new ReinforcementSpawnRule(_vision, _blockers, _clearance, _reachability, _structureClearance,
                new FixedMap(), _hazards);
            _vision.Register(TestPlayers.Human, CreateTransform(Vector3.zero), 500f);
            _vision.Register(TestPlayers.Enemy, CreateTransform(Vector3.zero), 500f);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_root);

        [Test]
        public void IsOpen_Hazard_BlocksEveryTeamAndUnitKind()
        {
            _hazards.Blocks = true;
            Assert.That(_rule.IsOpen(TestPlayers.Human, Vector3.zero), Is.False);
            Assert.That(_rule.IsOpen(TestPlayers.Enemy, Vector3.zero), Is.False);
            Assert.That(_rule.CanSpawnShip(TestPlayers.Human, ShipType.Arquitens, Vector3.zero), Is.False);
            Assert.That(_rule.CanSpawnStructure(TestPlayers.Human, Vector3.zero), Is.False);
            _hazards.Blocks = false;
            Assert.That(_rule.IsOpen(TestPlayers.Human, Vector3.zero), Is.True);
        }

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
            _blockers.Register(BattleAreaLayer.SpawnBlock, TestPlayers.Enemy, CreateTransform(Vector3.zero), 200f);

            Assert.That(_rule.IsOpen(TestPlayers.Human, new Vector3(100f, 0f, 0f)), Is.False);
            Assert.That(_rule.IsOpen(TestPlayers.Enemy, new Vector3(100f, 0f, 0f)), Is.True);
            Assert.That(_rule.IsOpen(TestPlayers.Human, new Vector3(300f, 0f, 0f)), Is.True);
        }

        [Test]
        public void IsOpen_NeutralAsteroid_BlocksEveryone()
        {
            _blockers.Register(BattleAreaLayer.SpawnBlock, PlayerId.None, CreateTransform(Vector3.zero), 200f);

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
        public void CanSpawnShip_WalledOffFromOwnStation_False()
        {
            _reachability.IsReachableResult = false;

            Assert.That(_rule.CanSpawnShip(TestPlayers.Human, ShipType.Arquitens, Vector3.zero), Is.False);
            Assert.That(_rule.IsOpen(TestPlayers.Human, Vector3.zero), Is.True);
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
            Transform relay = CreateTransform(new Vector3(20f, 0f, 0f));
            Transform site = CreateTransform(new Vector3(20f, 0f, 0f));
            _blockers.Register(BattleAreaLayer.StructureExclusion, TestPlayers.Human, relay, 10f);
            _blockers.Register(BattleAreaLayer.StructureExclusion, PlayerId.None, site, 10f);

            Assert.That(_rule.CanSpawnShip(TestPlayers.Human, ShipType.Arquitens, Vector3.zero), Is.True);

            Assert.That(_rule.CanSpawnStructure(TestPlayers.Human, Vector3.zero), Is.False);

            _blockers.Unregister(BattleAreaLayer.StructureExclusion, relay);

            Assert.That(_rule.CanSpawnStructure(TestPlayers.Human, Vector3.zero), Is.False);
        }

        [Test]
        public void CanSpawnStructure_HostileBlocker_False()
        {
            _blockers.Register(BattleAreaLayer.SpawnBlock, TestPlayers.Enemy, CreateTransform(Vector3.zero), 200f);

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

        private sealed class FixedReachability : IShipSpawnReachability
        {
            public bool IsReachableResult { get; set; } = true;

            public bool IsReachable(PlayerId owner, ShipType shipType, Vector3 position) => IsReachableResult;
        }

        private sealed class FixedStructureClearance : IStructureSpawnClearance
        {
            public bool IsClearResult { get; set; } = true;

            public float Radius => 10f;

            public bool IsClear(Vector3 position) => IsClearResult;
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
