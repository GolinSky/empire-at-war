using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Entities.UnitActions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Services.UnitOrders;
using EmpireAtWar.Ship;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class UnitOrderServiceTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private UnitOrderSettings _settings;
        private FakeLocator _locator;
        private FakeZones _zones;
        private UnitOrderService _orders;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<UnitOrderSettings>();
            _locator = new FakeLocator();
            _zones = new FakeZones();
            _orders = new UnitOrderService(_locator, _zones, _settings);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject obj in _objects) UnityEngine.Object.DestroyImmediate(obj);
            UnityEngine.Object.DestroyImmediate(_settings);
        }

        [TestCase(UnitActionId.Move)]
        [TestCase(UnitActionId.Attack)]
        [TestCase(UnitActionId.AttackMove)]
        [TestCase(UnitActionId.Stop)]
        [TestCase(UnitActionId.Guard)]
        [TestCase(UnitActionId.WaypointMove)]
        [TestCase(UnitActionId.Hunt)]
        [TestCase(UnitActionId.Retreat)]
        public void EveryAction_OnlyTouchesExplicitReceivers(UnitActionId action)
        {
            FakeEntity first = Entity(1, PlayerType.Player);
            FakeEntity second = Entity(2, PlayerType.Player);
            FakeEntity third = Entity(3, PlayerType.Player);
            FakeEntity enemy = Entity(4, PlayerType.Opponent);
            FakeEntity friendly = Entity(5, PlayerType.Player);
            UnitOrder issued = default;
            bool fired = false;
            _orders.OrderIssued += order => { issued = order; fired = true; };

            Issue(action, new IEntity[] { second }, enemy, friendly);
            Assert.That(first.Command.CallCount, Is.Zero);
            Assert.That(second.Command.CallCount, Is.EqualTo(1));
            Assert.That(third.Command.CallCount, Is.Zero);
            Assert.That(fired, Is.True);
            Assert.That(issued.Issuer, Is.EqualTo(PlayerType.Player));
            Assert.That(issued.Action, Is.EqualTo(action));

            first.Command.CallCount = 0;
            second.Command.CallCount = 0;
            third.Command.CallCount = 0;
            Issue(action, new IEntity[] { first, third }, enemy, friendly);
            Assert.That(first.Command.CallCount, Is.EqualTo(1));
            Assert.That(second.Command.CallCount, Is.Zero);
            Assert.That(third.Command.CallCount, Is.EqualTo(1));
        }

        [Test]
        public void UnsupportedReceiver_IsSkipped_IncludingMixedStationAttack()
        {
            FakeEntity ship = Entity(1, PlayerType.Player);
            FakeEntity station = Entity(2, PlayerType.Player,
                new[] { typeof(IFocusFireCommand), typeof(IStopCommand) });
            FakeEntity facility = Entity(3, PlayerType.Player, Array.Empty<Type>());
            FakeEntity enemy = Entity(4, PlayerType.Opponent);

            _orders.IssueAttack(new IEntity[] { ship, station, facility }, enemy);
            Assert.That(ship.Command.CallCount, Is.EqualTo(1));
            Assert.That(station.Command.CallCount, Is.EqualTo(1));
            Assert.That(facility.Command.CallCount, Is.Zero);
            Assert.That(station.Command.LastAction, Is.EqualTo(UnitActionId.Attack));
            _orders.IssueRetreat(new IEntity[] { ship, station, facility });
            Assert.That(ship.Command.CallCount, Is.EqualTo(2));
            Assert.That(station.Command.CallCount, Is.EqualTo(1));
        }

        [Test]
        public void Move_UsesCompactFormationOrExplicitDestinations()
        {
            FakeEntity first = Entity(1, PlayerType.Player);
            FakeEntity second = Entity(2, PlayerType.Player);
            Vector3 destination = new Vector3(100f, 0f, 100f);
            _orders.IssueMove(new IEntity[] { first, second }, destination);
            Assert.That(first.Command.LastPoint, Is.Not.EqualTo(second.Command.LastPoint));
            Assert.That(Vector3.Distance(first.Command.LastPoint, second.Command.LastPoint),
                Is.GreaterThanOrEqualTo(10f));

            Vector3[] slots = { new Vector3(10f, 0f, 20f),
                new Vector3(30f, 0f, 40f) };
            _orders.IssueMove(new IEntity[] { first, second }, slots);
            Assert.That(first.Command.LastPoint, Is.EqualTo(slots[0]));
            Assert.That(second.Command.LastPoint, Is.EqualTo(slots[1]));
        }

        [Test]
        public void Retreat_UsesOwnStationThenDefaultZoneWhenStationDestroyed()
        {
            FakeEntity ship = Entity(1, PlayerType.Opponent);
            FakeEntity station = Entity(2, PlayerType.Opponent,
                Array.Empty<Type>(), new FakeStationModel());
            station.Health.Transform.position = new Vector3(200f, 0f, 0f);
            _locator.EntitiesList.Add(station);
            _orders.IssueRetreat(new IEntity[] { ship });
            Assert.That(ship.Command.LastPoint.x, Is.LessThan(200f));
            station.Health.IsDestroyedValue = true;
            _orders.IssueRetreat(new IEntity[] { ship });
            Assert.That(ship.Command.LastPoint, Is.EqualTo(_zones.Center));
            Assert.That(_zones.LastSide, Is.EqualTo(PlayerType.Opponent));
        }

        private FakeEntity Entity(long id, PlayerType side, Type[] commands = null,
            IModelObserver model = null)
        {
            GameObject obj = new GameObject("Unit " + id);
            _objects.Add(obj);
            FakeEntity entity = new FakeEntity(id, side, new FakeHealth(obj.transform),
                model, commands);
            return entity;
        }

        private void Issue(UnitActionId action, IReadOnlyList<IEntity> receivers,
            IEntity enemy, IEntity friendly)
        {
            Vector3 point = new Vector3(50f, 0f, 50f);
            switch (action)
            {
                case UnitActionId.Move: _orders.IssueMove(receivers, point); break;
                case UnitActionId.Attack: _orders.IssueAttack(receivers, enemy); break;
                case UnitActionId.AttackMove: _orders.IssueAttackMove(receivers, point); break;
                case UnitActionId.Stop: _orders.IssueStop(receivers); break;
                case UnitActionId.Guard: _orders.IssueGuard(receivers, friendly); break;
                case UnitActionId.WaypointMove:
                    _orders.IssueWaypointMove(receivers, new[] { point, point + Vector3.right });
                    break;
                case UnitActionId.Hunt: _orders.IssueHunt(receivers); break;
                case UnitActionId.Retreat: _orders.IssueRetreat(receivers); break;
            }
        }

        private sealed class FakeEntity : IEntity
        {
            private readonly HashSet<Type> _commands;
            public FakeEntity(long id, PlayerType side, FakeHealth health,
                IModelObserver model, Type[] commands)
            {
                Id = id;
                PlayerType = side;
                Health = health;
                Model = model;
                Command = new FakeCommand();
                _commands = commands == null ? null : new HashSet<Type>(commands);
            }
            public long Id { get; }
            public PlayerType PlayerType { get; }
            public IModelObserver Model { get; }
            public FakeHealth Health { get; }
            public IHealthModelObserver HealthModel => Health;
            public FakeCommand Command { get; }
            public bool TryGetCommand<TCommand>(out TCommand command)
                where TCommand : IEntityCommand
            {
                if (_commands == null || _commands.Contains(typeof(TCommand)))
                {
                    command = (TCommand)(IEntityCommand)Command;
                    return true;
                }
                command = default;
                return false;
            }
        }

        private sealed class FakeCommand : IMoveCommand, IAttackCommand,
            IAttackMoveCommand, IStopCommand, IGuardCommand, IWaypointMoveCommand,
            IHuntCommand, IRetreatCommand, IFocusFireCommand
        {
            public int CallCount { get; set; }
            public UnitActionId LastAction { get; private set; }
            public Vector3 LastPoint { get; private set; }
            public Vector3 WorldPosition => Vector3.zero;
            public float NavigationRadius => 5f;
            public void MoveTo(Vector2 point) => Record(UnitActionId.Move, point);
            public void MoveTo(Vector3 point) => Record(UnitActionId.Move, point);
            public void Attack(IEntity target, Vector3 offset) =>
                Record(UnitActionId.Attack, offset);
            public void FocusFire(IEntity target) => Record(UnitActionId.Attack);
            public void AttackMoveTo(Vector3 point) => Record(UnitActionId.AttackMove, point);
            public void Stop() => Record(UnitActionId.Stop);
            public void Guard(IEntity target, Vector3 offset) =>
                Record(UnitActionId.Guard, offset);
            public void MoveAlong(IReadOnlyList<Vector3> points) =>
                Record(UnitActionId.WaypointMove, points[0]);
            public void Hunt() => Record(UnitActionId.Hunt);
            public void Retreat(Vector3 point) =>
                Record(UnitActionId.Retreat, point);
            private void Record(UnitActionId action, Vector3 point = default)
            { CallCount++; LastAction = action; LastPoint = point; }
        }

        private sealed class FakeHealth : IHealthModelObserver
        {
            public FakeHealth(Transform transform) { Transform = transform; }
            public bool IsDestroyedValue { get; set; }
            public event Action OnDestroy;
            public event Action OnValueChanged;
            public HardPointModel[] HardPointModels => Array.Empty<HardPointModel>();
            public float Armor => 1f;
            public float ArmorPercentage => 1f;
            public float Shields => 1f;
            public float ShieldPercentage => 1f;
            public bool IsDestroyed => IsDestroyedValue;
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public PlayerType PlayerType => PlayerType.Player;
            public Transform Transform { get; }
            public bool HasShields => true;
            public IHardPointModel[] GetShipUnits(HardPointType type) =>
                Array.Empty<IHardPointModel>();
        }

        private sealed class FakeStationModel : ISpaceStationModelObserver { }

        private sealed class FakeLocator : IEntityLocator
        {
            public event Action<IEntity> EntityAdded;
            public event Action<IEntity> EntityRemoved;
            public string Id => nameof(FakeLocator);
            public List<IEntity> EntitiesList { get; } = new List<IEntity>();
            public IReadOnlyCollection<IEntity> Entities => EntitiesList;
            public void AddEntity(IEntity entity) => EntitiesList.Add(entity);
            public void RemoveEntity(IEntity entity) => EntitiesList.Remove(entity);
            public IEntity GetEntity(long id) => EntitiesList.Find(entity => entity.Id == id);
            public bool TryGetEntity(long id, out IEntity entity)
            { entity = GetEntity(id); return entity != null; }
            public bool IsStationOperational(PlayerType side) => true;
            public bool TryGetEntity(RaycastHit hit, out IEntity entity)
            { entity = null; return false; }
            public bool TryGetEntity(Collider collider, out IEntity entity)
            { entity = null; return false; }
        }

        private sealed class FakeZones : IReinforcementZonesSystem
        {
            public event Action OwnershipChanged;
            public Vector3 Center => new Vector3(-100f, 0f, 50f);
            public PlayerType LastSide { get; private set; }
            public bool TryGetDefaultZoneCenter(PlayerType side, out Vector3 point)
            { LastSide = side; point = Center; return true; }
            public bool IsPositionInAnyZone(Vector3 point, float clearance = 0f) => false;
            public bool IsPositionInOwnedZone(PlayerType side, Vector3 point) => false;
            public int GetOwnedCapturableZoneCount(PlayerType side) => 0;
            public bool IsShipSpawnPositionClear(ShipType type, Vector3 point) => true;
            public void CopyOwnedCapturableZoneCenters(PlayerType side, List<Vector3> dest)
                => dest.Clear();
            public bool TryGetDefaultSpawnPosition(PlayerType side, out Vector3 point)
            { point = default; return false; }
            public bool TryGetDefaultZoneExitPosition(PlayerType side, Vector3 ship,
                float radius, out Vector3 point) { point = default; return false; }
            public bool TryGetRandomSpawnPosition(PlayerType side, ShipType type,
                out Vector3 point) { point = default; return false; }
            public bool TryGetCaptureTarget(PlayerType side, Vector3 origin,
                out Vector3 point) { point = default; return false; }
        }
    }
}
