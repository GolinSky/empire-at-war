using System;
using System.Collections.Generic;
using EmpireAtWar.Commands.Game;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Entities.UnitActions;
using EmpireAtWar.Entities.UnitActions.Controller;
using EmpireAtWar.Entities.UnitActions.Model;
using EmpireAtWar.Entities.UnitActions.Ui;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.SkirmishGame;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.InputService;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.UnitOrders;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class UnitActionsPresenterTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private FakeView _view;
        private FakeSelection _selection;
        private FakeInput _input;
        private FakeAbilities _abilities;
        private UnitActionTargetingModel _targeting;
        private FakeHandler _handler;
        private FakeOrders _orders;
        private FakeSession _session;
        private UnitActionsPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _view = new FakeView();
            _selection = new FakeSelection();
            _input = new FakeInput();
            _abilities = new FakeAbilities();
            _targeting = new UnitActionTargetingModel();
            _handler = new FakeHandler(_targeting);
            _orders = new FakeOrders();
            _session = new FakeSession();
            _presenter = new UnitActionsPresenter(new FakeProvider(_view),
                _selection, _input, _abilities, _targeting, _handler, _orders, _session);
            _presenter.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _presenter.LateDispose();
            foreach (GameObject obj in _objects) UnityEngine.Object.DestroyImmediate(obj);
        }

        [Test]
        public void Availability_FollowsCommandsAcrossShipStationAndFacility()
        {
            FakeEntity ship = Entity(1, null);
            _selection.Select(ship);
            foreach (UnitActionId action in Enum.GetValues(typeof(UnitActionId)))
                Assert.That(_view.Available[action], Is.True, action.ToString());
            Assert.That(_view.Visible, Is.True);

            FakeEntity station = Entity(2,
                new[] { typeof(IFocusFireCommand), typeof(IStopCommand) });
            _selection.Select(station);
            Assert.That(_view.Available[UnitActionId.Attack], Is.True);
            Assert.That(_view.Available[UnitActionId.Stop], Is.True);
            Assert.That(_view.Available[UnitActionId.Move], Is.False);
            Assert.That(_view.Available[UnitActionId.Retreat], Is.False);

            FakeEntity facility = Entity(3, Array.Empty<Type>());
            _selection.Select(facility);
            Assert.That(_view.Visible, Is.False);
        }

        [Test]
        public void PendingAction_CancelsOnSameButtonEscapeAbilityAndSelectionChange()
        {
            FakeEntity ship = Entity(1, null);
            _selection.Select(ship);
            _view.Press(UnitActionId.Move);
            Assert.That(_targeting.Pending, Is.EqualTo(UnitActionId.Move));
            Assert.That(_abilities.CancelCount, Is.EqualTo(1));
            _view.Press(UnitActionId.Move);
            Assert.That(_targeting.Pending, Is.Null);

            _view.Press(UnitActionId.Guard);
            _input.Escape();
            Assert.That(_targeting.Pending, Is.Null);
            _view.Press(UnitActionId.Attack);
            _abilities.StartTargeting();
            Assert.That(_targeting.Pending, Is.Null);
            _abilities.CancelTargeting();
            _view.Press(UnitActionId.Move);
            _selection.Select(Entity(2, null));
            Assert.That(_targeting.Pending, Is.Null);
        }

        [Test]
        public void WaypointToggleFinishesAndStopDiscardsPendingPath()
        {
            _selection.Select(Entity(1, null));
            _view.Press(UnitActionId.WaypointMove);
            _targeting.AddWaypoint(new FormationPoint(1f, 2f));
            _view.Press(UnitActionId.WaypointMove);
            Assert.That(_handler.FinishCount, Is.EqualTo(1));
            Assert.That(_targeting.Pending, Is.Null);

            _view.Press(UnitActionId.WaypointMove);
            _targeting.AddWaypoint(new FormationPoint(3f, 4f));
            _view.Press(UnitActionId.Stop);
            Assert.That(_targeting.Pending, Is.Null);
            Assert.That(_targeting.Waypoints, Is.Empty);
            Assert.That(_orders.LastAction, Is.EqualTo(UnitActionId.Stop));
        }

        [Test]
        public void RetreatButton_CancelsOnlyWhenAllCapableReceiversPending()
        {
            FakeEntity ship = Entity(1, null);
            _selection.Select(ship);
            ship.Command.IsRetreatPending = true;
            ship.Command.RetreatRemaining = 2.4f;
            _presenter.Tick();
            Assert.That(_view.Countdown, Is.EqualTo(2.4f));
            _view.Press(UnitActionId.Retreat);
            Assert.That(_orders.CancelCount, Is.EqualTo(1));
            ship.Command.IsRetreatPending = false;
            _view.Press(UnitActionId.Retreat);
            Assert.That(_orders.LastAction, Is.EqualTo(UnitActionId.Retreat));
        }

        private FakeEntity Entity(long id, Type[] commands)
        {
            GameObject obj = new GameObject("Unit " + id);
            _objects.Add(obj);
            return new FakeEntity(id, obj.transform, commands);
        }

        private sealed class FakeProvider : IUnitActionsViewProvider
        {
            public FakeProvider(IUnitActionsView view) { UnitActionsView = view; }
            public IUnitActionsView UnitActionsView { get; }
        }

        private sealed class FakeView : IUnitActionsView
        {
            public event Action<UnitActionId> ActionPressed;
            public Dictionary<UnitActionId, bool> Available { get; } =
                new Dictionary<UnitActionId, bool>();
            public bool Visible { get; private set; }
            public UnitActionId? Pending { get; private set; }
            public float? Countdown { get; private set; }
            public void Initialize() { }
            public void Dispose() { }
            public void Press(UnitActionId id) => ActionPressed?.Invoke(id);
            public void SetVisible(bool visible) => Visible = visible;
            public void SetAvailable(UnitActionId id, bool available) => Available[id] = available;
            public void SetPending(UnitActionId? id) => Pending = id;
            public void SetRetreatCountdown(float? value) => Countdown = value;
        }

        private sealed class FakeSelection : ISelectionService, ISelectionContext, ISelectionSubject
        {
            private readonly List<IEntity> _entities = new List<IEntity>();
            private readonly List<IObserver<ISelectionSubject>> _observers =
                new List<IObserver<ISelectionSubject>>();
            public string Id => nameof(FakeSelection);
            public ISelectionContext PlayerSelectionContext => this;
            public ISelectionContext EnemySelectionContext => null;
            public PlayerType UpdatedType => PlayerType.Player;
            public IEntity Entity => _entities.Count > 0 ? _entities[0] : null;
            public IReadOnlyList<IEntity> Entities => _entities;
            public IEntitySelectionCommand SelectionCommand => null;
            public SelectionType SelectionType => SelectionType.Ship;
            public bool HasSelectable => _entities.Count > 0;
            public int Count => _entities.Count;
            public PlayerType PlayerType => PlayerType.Player;
            public bool Contains(IEntity entity) => _entities.Contains(entity);
            public void Select(IEntity entity)
            {
                _entities.Clear();
                _entities.Add(entity);
                foreach (IObserver<ISelectionSubject> observer in _observers)
                    observer.UpdateState(this);
            }
            public void RemoveSelectable(ISelectionContext context) { }
            public void SelectCurrentShipsByType(ShipType type) { }
            public void AddObserver(IObserver<ISelectionSubject> observer) => _observers.Add(observer);
            public void RemoveObserver(IObserver<ISelectionSubject> observer) => _observers.Remove(observer);
        }

        private sealed class FakeInput : IInputService
        {
            public event Action<Vector2> OnSwipe;
            public event Action<Vector2> OnCameraPan;
            public event Action OnLeftMousePressed;
            public event Action<Vector2> OnPrimaryDragStarted;
            public event Action<Vector2> OnPrimaryDragChanged;
            public event Action<Vector2> OnPrimaryDragEnded;
            public event Action OnEscapePressed;
            public event Action OnWaypointModifierReleased;
            public event Action OnSelectAllUnitsPressed;
            public event Action OnSelectVisibleUnitsPressed;
            public event Action<bool> OnBlocked;
            public event Action<InputType, TouchPhase, Vector2> OnInput;
            public event Action<Vector2> OnEndDrag;
            public event Action<float> OnZoom;
            public string Id => nameof(FakeInput);
            public TouchPhase CurrentTouchPhase => default;
            public Vector2 TouchPosition => default;
            public bool SupportsHover => true;
            public bool IsWaypointModifierPressed => false;
            public Vector2 CameraMove => default;
            public int TapCount => 1;
            public void Escape() => OnEscapePressed?.Invoke();
        }

        private sealed class FakeAbilities : IShipAbilityTargeting
        {
            public event Action TargetingChanged;
            public bool IsWaitingForTarget { get; private set; }
            public int CancelCount { get; private set; }
            public void StartTargeting()
            { IsWaitingForTarget = true; TargetingChanged?.Invoke(); }
            public void SubmitTarget(IEntity target) { }
            public void CancelTargeting()
            { CancelCount++; IsWaitingForTarget = false; TargetingChanged?.Invoke(); }
        }

        private sealed class FakeHandler : IPlayerOrderInputHandler
        {
            private readonly UnitActionTargetingModel _targeting;
            public FakeHandler(UnitActionTargetingModel targeting) { _targeting = targeting; }
            public int FinishCount { get; private set; }
            public void FinishWaypoints() { FinishCount++; _targeting.Cancel(); }
        }

        private sealed class FakeOrders : IUnitOrderService
        {
            public event Action<UnitOrder> OrderIssued { add { } remove { } }
            public UnitActionId? LastAction { get; private set; }
            public int CancelCount { get; private set; }
            public void IssueMove(IReadOnlyList<IEntity> units, Vector3 point) => LastAction = UnitActionId.Move;
            public void IssueMove(IReadOnlyList<IEntity> units, IReadOnlyList<Vector3> points) => LastAction = UnitActionId.Move;
            public void IssueAttack(IReadOnlyList<IEntity> units, IEntity target) => LastAction = UnitActionId.Attack;
            public void IssueAttack(IReadOnlyList<IEntity> units, IEntity target,
                IReadOnlyList<Vector3> offsets) => LastAction = UnitActionId.Attack;
            public void IssueAttackMove(IReadOnlyList<IEntity> units, Vector3 point) => LastAction = UnitActionId.AttackMove;
            public void IssueStop(IReadOnlyList<IEntity> units) => LastAction = UnitActionId.Stop;
            public void IssueGuard(IReadOnlyList<IEntity> units, IEntity target) => LastAction = UnitActionId.Guard;
            public void IssueGuard(IReadOnlyList<IEntity> units, IEntity target,
                IReadOnlyList<Vector3> offsets) => LastAction = UnitActionId.Guard;
            public void IssueWaypointMove(IReadOnlyList<IEntity> units,
                IReadOnlyList<Vector3> points) => LastAction = UnitActionId.WaypointMove;
            public void IssueHunt(IReadOnlyList<IEntity> units) => LastAction = UnitActionId.Hunt;
            public void IssueRetreat(IReadOnlyList<IEntity> units) => LastAction = UnitActionId.Retreat;
            public void CancelRetreat(IReadOnlyList<IEntity> units) => CancelCount++;
        }

        private sealed class FakeSession : ISkirmishSessionModelObserver
        {
            public event Action<GameTimeMode> OnGameTimeModeChanged;
            public GameTimeMode GameTimeMode => default;
            public bool IsBattleEnded => false;
        }

        private sealed class FakeEntity : IEntity
        {
            private readonly HashSet<Type> _allowed;
            public FakeEntity(long id, Transform transform, Type[] commands)
            {
                Id = id;
                HealthModel = new FakeHealth(transform);
                Command = new FakeCommand();
                _allowed = commands == null ? null : new HashSet<Type>(commands);
            }
            public long Id { get; }
            public IModelObserver Model => null;
            public IHealthModelObserver HealthModel { get; }
            public PlayerType PlayerType => PlayerType.Player;
            public FakeCommand Command { get; }
            public bool TryGetCommand<TCommand>(out TCommand command)
                where TCommand : IEntityCommand
            {
                if (_allowed == null || _allowed.Contains(typeof(TCommand)))
                { command = (TCommand)(IEntityCommand)Command; return true; }
                command = default;
                return false;
            }
        }

        private sealed class FakeCommand : IMoveCommand, IAttackCommand,
            IAttackMoveCommand, IStopCommand, IGuardCommand, IWaypointMoveCommand,
            IHuntCommand, IRetreatCommand, IFocusFireCommand
        {
            public Vector3 WorldPosition => Vector3.zero;
            public float NavigationRadius => 5f;
            public bool IsRetreatPending { get; set; }
            public float RetreatRemaining { get; set; }
            public void MoveTo(Vector2 point) { }
            public void MoveTo(Vector3 point) { }
            public void Attack(IEntity target, Vector3 offset) { }
            public void FocusFire(IEntity target) { }
            public void AttackMoveTo(Vector3 point) { }
            public void Stop() { }
            public void Guard(IEntity target, Vector3 offset) { }
            public void MoveAlong(IReadOnlyList<Vector3> waypoints) { }
            public void Hunt() { }
            public void Retreat(Vector3 point, float delay) { }
            public void CancelRetreat() { }
        }

        private sealed class FakeHealth : IHealthModelObserver
        {
            public FakeHealth(Transform transform) { Transform = transform; }
            public event Action OnDestroy;
            public event Action OnValueChanged;
            public HardPointModel[] HardPointModels => Array.Empty<HardPointModel>();
            public float Armor => 1f;
            public float ArmorPercentage => 1f;
            public float Shields => 1f;
            public float ShieldPercentage => 1f;
            public bool IsDestroyed => false;
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public PlayerType PlayerType => PlayerType.Player;
            public Transform Transform { get; }
            public bool HasShields => true;
            public IHardPointModel[] GetShipUnits(HardPointType type) =>
                Array.Empty<IHardPointModel>();
        }
    }
}
