using System;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Entities.UnitActions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Services.UnitOrders;
using EmpireAtWar.Entities.BaseEntity.Orders;
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
        private UnitOrderService _unitOrderService;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<UnitOrderSettings>();
            _locator = new FakeLocator();
            _zones = new FakeZones();
            _unitOrderService = new UnitOrderService(_locator, _zones, _settings);
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
            FakeEntity first = Entity(1, TestPlayers.Human);
            FakeEntity second = Entity(2, TestPlayers.Human);
            FakeEntity third = Entity(3, TestPlayers.Human);
            FakeEntity enemy = Entity(4, TestPlayers.Enemy);
            FakeEntity friendly = Entity(5, TestPlayers.Human);
            UnitOrder issued = default;
            bool fired = false;
            _unitOrderService.OrderIssued += order => { issued = order; fired = true; };

            Issue(action, new IEntity[] { second }, enemy, friendly);
            Assert.That(first.Command.CallCount, Is.Zero);
            Assert.That(second.Command.CallCount, Is.EqualTo(1));
            Assert.That(third.Command.CallCount, Is.Zero);
            Assert.That(fired, Is.True);
            Assert.That(issued.Issuer, Is.EqualTo(TestPlayers.Human));
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
            FakeEntity ship = Entity(1, TestPlayers.Human);
            FakeEntity station = Entity(2, TestPlayers.Human,
                new[] { typeof(IFocusFireFacade), typeof(IStopFacade) });
            FakeEntity facility = Entity(3, TestPlayers.Human, Array.Empty<Type>());
            FakeEntity enemy = Entity(4, TestPlayers.Enemy);

            _unitOrderService.IssueAttack(new IEntity[] { ship, station, facility }, enemy);
            Assert.That(ship.Command.CallCount, Is.EqualTo(1));
            Assert.That(station.Command.CallCount, Is.EqualTo(1));
            Assert.That(facility.Command.CallCount, Is.Zero);
            Assert.That(station.Command.LastAction, Is.EqualTo(UnitActionId.Attack));
            _unitOrderService.IssueRetreat(new IEntity[] { ship, station, facility });
            Assert.That(ship.Command.CallCount, Is.EqualTo(2));
            Assert.That(station.Command.CallCount, Is.EqualTo(1));
        }

        [Test]
        public void Move_UsesCompactFormationOrExplicitDestinations()
        {
            FakeEntity first = Entity(1, TestPlayers.Human);
            FakeEntity second = Entity(2, TestPlayers.Human);
            Vector3 destination = new Vector3(100f, 0f, 100f);
            _unitOrderService.IssueMove(new IEntity[] { first, second }, destination);
            Assert.That(first.Command.LastPoint, Is.Not.EqualTo(second.Command.LastPoint));
            Assert.That(Vector3.Distance(first.Command.LastPoint, second.Command.LastPoint),
                Is.GreaterThanOrEqualTo(10f));

            Vector3[] slots = { new Vector3(10f, 0f, 20f),
                new Vector3(30f, 0f, 40f) };
            _unitOrderService.IssueMove(new IEntity[] { first, second }, slots);
            Assert.That(first.Command.LastPoint, Is.EqualTo(slots[0]));
            Assert.That(second.Command.LastPoint, Is.EqualTo(slots[1]));
        }

        [Test]
        public void Retreat_UsesOwnStationThenDefaultZoneWhenStationDestroyed()
        {
            FakeEntity ship = Entity(1, TestPlayers.Enemy);
            FakeEntity station = Entity(2, TestPlayers.Enemy,
                Array.Empty<Type>(), new PlayerBaseFacade());
            station.Health.Transform.position = new Vector3(200f, 0f, 0f);
            _locator.EntitiesList.Add(station);
            _unitOrderService.IssueRetreat(new IEntity[] { ship });
            Assert.That(ship.Command.LastPoint.x, Is.LessThan(200f));
            station.Health.IsDestroyedValue = true;
            _unitOrderService.IssueRetreat(new IEntity[] { ship });
            Assert.That(ship.Command.LastPoint, Is.EqualTo(_zones.Center));
            Assert.That(_zones.LastSide, Is.EqualTo(TestPlayers.Enemy));
        }

        private FakeEntity Entity(long id, PlayerId side, Type[] commands = null,
            IEntityFacade role = null)
        {
            GameObject obj = new GameObject("Unit " + id);
            _objects.Add(obj);
            FakeEntity entity = new FakeEntity(id: id, side: side, health: new FakeHealth(obj.transform),
                role: role, commands: commands);
            return entity;
        }

        private void Issue(UnitActionId action, IReadOnlyList<IEntity> receivers,
            IEntity enemy, IEntity friendly)
        {
            Vector3 point = new Vector3(50f, 0f, 50f);
            switch (action)
            {
                case UnitActionId.Move: _unitOrderService.IssueMove(receivers, point); break;
                case UnitActionId.Attack: _unitOrderService.IssueAttack(receivers, enemy); break;
                case UnitActionId.AttackMove: _unitOrderService.IssueAttackMove(receivers, point); break;
                case UnitActionId.Stop: _unitOrderService.IssueStop(receivers); break;
                case UnitActionId.Guard: _unitOrderService.IssueGuard(receivers, friendly); break;
                case UnitActionId.WaypointMove:
                    _unitOrderService.IssueWaypointMove(receivers, new[] { point, point + Vector3.right });
                    break;
                case UnitActionId.Hunt: _unitOrderService.IssueHunt(receivers); break;
                case UnitActionId.Retreat: _unitOrderService.IssueRetreat(receivers); break;
            }
        }

        private sealed class FakeEntity : IEntity
        {
            private readonly IEntityFacade _role;

            private readonly HashSet<Type> _commands;

            public long Id { get; }
            public PlayerId Owner { get; }
            public FakeHealth Health { get; }
            public IHealthModelObserver HealthModel => Health;
            public FakeCommand Command { get; }

            public FakeEntity(IEntityFacade role, FakeHealth health, Type[] commands,
                PlayerId side, long id)
            {
                Id = id;
                Owner = side;
                Health = health;
                _role = role;
                Command = new FakeCommand();
                _commands = commands == null ? null : new HashSet<Type>(commands);
            }

            public TCommand GetFacade<TCommand>() where TCommand : IEntityFacade
            { TryGetFacade(out TCommand facade); return facade; }

            public bool TryGetFacade<TCommand>(out TCommand command)
                where TCommand : IEntityFacade
            { if (HealthModel is TCommand transformFacade) { command = transformFacade; return true; }
                if (_role is TCommand roleFacade)
                {
                    command = roleFacade;
                    return true;
                }
                if ((_commands == null || _commands.Contains(typeof(TCommand))) && Command is TCommand allowed)
                {
                    command = allowed;
                    return true;
                }
                command = default;
                return false;
            }
        }

        private sealed class FakeCommand : IMoveFacade, IAttackFacade,
            IAttackMoveFacade, IStopFacade, IGuardFacade, IWaypointMoveFacade,
            IHuntFacade, IRetreatFacade, IFocusFireFacade
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

            public void AttackMoveTo(Vector3 point, AttackMoveEngagement engagement) => Record(UnitActionId.AttackMove, point);

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

        private sealed class FakeHealth : IHealthModelObserver, IEntityTransformFacade
        {
            public event Action OnDestroy { add { } remove { } }

            public event Action OnValueChanged { add { } remove { } }

            public bool IsDestroyedValue { get; set; }
            public HardPointModel[] HardPointModels => Array.Empty<HardPointModel>();
            public float Hull => 1f;
            public ShipClass ShipClass => ShipClass.Capital;
            public bool HasLiveHardPoints => true;
            public float HullPercentage => 1f;
            public float Shields => 1f;
            public float ShieldPercentage => 1f;
            public bool IsDestroyed => IsDestroyedValue;
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public PlayerId Owner => TestPlayers.Human;
            public Transform Transform { get; }
            public bool HasShields => true;

            public FakeHealth(Transform transform) { Transform = transform; }

            public IHardPointModel[] GetShipUnits(HardPointType type) =>
                Array.Empty<IHardPointModel>();
        }

        private sealed class FakeLocator : IEntityLocator
        {
            public event Action<IEntity> EntityAdded { add { } remove { } }

            public event Action<IEntity> EntityRemoved { add { } remove { } }

            public string Id => nameof(FakeLocator);
            public List<IEntity> EntitiesList { get; } = new List<IEntity>();
            public IReadOnlyCollection<IEntity> Entities => EntitiesList;

            public void AddEntity(IEntity entity) => EntitiesList.Add(entity);

            public void RemoveEntity(IEntity entity) => EntitiesList.Remove(entity);

            public IEntity GetEntity(long id) => EntitiesList.Find(entity => entity.Id == id);

            public bool TryGetEntity(long id, out IEntity entity)
            { entity = GetEntity(id); return entity != null; }

            public bool TryGetEntity(RaycastHit hit, out IEntity entity)
            { entity = null; return false; }

            public bool TryGetEntity(Collider collider, out IEntity entity)
            { entity = null; return false; }
        }

        private sealed class FakeZones : IReinforcementZonesSystem
        {
            public event Action OwnershipChanged { add { } remove { } }

            public Vector3 Center => new Vector3(-100f, 0f, 50f);
            public PlayerId LastSide { get; private set; }

            public bool TryGetDefaultZoneCenter(PlayerId side, out Vector3 point)
            { LastSide = side; point = Center; return true; }

            public bool IsPositionInAnyZone(Vector3 point, float clearance = 0f) => false;

            public bool IsPositionInAlliedZone(PlayerId side, Vector3 point) => false;

            public int GetOwnedCapturableZoneCount(PlayerId side) => 0;

            public void CopyOwnedCapturableZoneBounds(PlayerId side, List<Bounds> dest)
                => dest.Clear();

            public bool TryGetDefaultZoneExitPosition(PlayerId side, Vector3 ship,
                float radius, out Vector3 point) { point = default; return false; }

            public bool TryGetCaptureTarget(PlayerId side, Vector3 origin,
                out Vector3 point) { point = default; return false; }
        }
    }
}
