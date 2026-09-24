using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Selection.Marquee;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using EmpireAtWar.Entities.UnitActions;
using EmpireAtWar.Entities.UnitActions.Model;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.InputService;
using EmpireAtWar.Services.Layer;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.UnitOrders;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    public sealed class PlayerOrderInputHandlerTests
    {
        private GameObject _view;
        private FakeEntity _receiver;
        private FakeEntity _enemy;
        private FakeInput _input;
        private FakeSelection _selection;
        private FakeQuery _query;
        private FakeAbilities _abilities;
        private UnitActionTargetingModel _targeting;
        private FakeOrders _orders;
        private FakeCamera _camera;
        private PlayerOrderInputHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _view = new GameObject("Test entity");
            _receiver = new FakeEntity(1, PlayerType.Player, _view.transform);
            _enemy = new FakeEntity(2, PlayerType.Opponent, _view.transform);
            _input = new FakeInput();
            _selection = new FakeSelection(_receiver);
            _query = new FakeQuery(_enemy);
            _abilities = new FakeAbilities();
            _targeting = new UnitActionTargetingModel();
            _orders = new FakeOrders();
            _camera = new FakeCamera();
            _handler = new PlayerOrderInputHandler(_input, _selection, _query,
                _camera, null, _abilities, _targeting, _orders);
            _handler.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _handler.LateDispose();
            UnityEngine.Object.DestroyImmediate(_view);
        }

        [Test]
        public void RightClickPriority_AbilityThenPendingThenDefault()
        {
            _targeting.Start(UnitActionId.Attack);
            _abilities.IsWaitingForTarget = true;
            _input.RightClick(Vector2.one);
            Assert.That(_abilities.Submitted, Is.SameAs(_enemy));
            Assert.That(_orders.LastAction, Is.Null);
            Assert.That(_targeting.Pending, Is.EqualTo(UnitActionId.Attack));

            _abilities.IsWaitingForTarget = false;
            _input.RightClick(Vector2.one);
            Assert.That(_orders.LastAction, Is.EqualTo(UnitActionId.Attack));
            Assert.That(_targeting.Pending, Is.Null);
            _orders.LastAction = null;
            _input.RightClick(Vector2.one);
            Assert.That(_orders.LastAction, Is.EqualTo(UnitActionId.Attack));
        }

        [Test]
        public void AltWaypoints_AreIssuedOnModifierRelease()
        {
            _query.ReturnEnemy = false;
            _input.IsWaypointModifierPressed = true;
            _input.RightClick(Vector2.zero);
            _input.RightClick(new Vector2(2f, 3f));
            Assert.That(_targeting.Waypoints.Count, Is.EqualTo(2));
            Assert.That(_orders.LastAction, Is.Null);
            _input.ReleaseAlt();
            Assert.That(_orders.LastAction, Is.EqualTo(UnitActionId.WaypointMove));
            Assert.That(_orders.WaypointCount, Is.EqualTo(2));
            Assert.That(_targeting.Pending, Is.Null);
        }

        [Test]
        public void PendingAttack_IgnoresInvalidClickWithoutMoving()
        {
            _targeting.Start(UnitActionId.Attack);
            _query.ReturnEnemy = false;
            _input.RightClick(Vector2.zero);
            Assert.That(_orders.LastAction, Is.Null);
            Assert.That(_targeting.Pending, Is.EqualTo(UnitActionId.Attack));
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
            public TouchPhase CurrentTouchPhase => TouchPhase.Ended;
            public Vector2 TouchPosition => default;
            public bool SupportsHover => true;
            public bool IsWaypointModifierPressed { get; set; }
            public Vector2 CameraMove => default;
            public int TapCount => 1;
            public void RightClick(Vector2 point) =>
                OnInput?.Invoke(InputType.ShipInput, TouchPhase.Ended, point);
            public void ReleaseAlt()
            { IsWaypointModifierPressed = false; OnWaypointModifierReleased?.Invoke(); }
        }

        private sealed class FakeSelection : ISelectionService, ISelectionContext
        {
            public FakeSelection(IEntity entity) { Entity = entity; }
            public string Id => nameof(FakeSelection);
            public IEntity Entity { get; }
            public IReadOnlyList<IEntity> Entities => new[] { Entity };
            public IEntitySelectionCommand SelectionCommand => null;
            public SelectionType SelectionType => SelectionType.Ship;
            public bool HasSelectable => true;
            public int Count => 1;
            public PlayerType PlayerType => PlayerType.Player;
            public bool Contains(IEntity entity) => entity.Id == Entity.Id;
            public ISelectionContext PlayerSelectionContext => this;
            public ISelectionContext EnemySelectionContext => null;
            public void RemoveSelectable(ISelectionContext context) { }
            public void SelectCurrentShipsByType(ShipType type) { }
            public void AddObserver(IObserver<ISelectionSubject> observer) { }
            public void RemoveObserver(IObserver<ISelectionSubject> observer) { }
        }

        private sealed class FakeQuery : ISelectionQuery
        {
            private readonly IEntity _enemy;
            public FakeQuery(IEntity enemy) { _enemy = enemy; }
            public bool ReturnEnemy { get; set; } = true;
            public bool TryFindAt(Vector2 point, out SelectionEntry result)
            {
                result = ReturnEnemy ? new SelectionEntry(_enemy, null) : default;
                return ReturnEnemy;
            }
            public void CollectSameShipType(SelectionEntry entry, ICollection<SelectionEntry> result) { }
            public void CollectAllPlayerUnits(ICollection<SelectionEntry> result) { }
            public void CollectVisiblePlayerUnits(ICollection<SelectionEntry> result) { }
            public void CollectInside(MarqueeRectangle rect, ICollection<SelectionEntry> result) { }
        }

        [Test]
        public void DefaultMove_ProjectsClickOntoReceiverHeight()
        {
            _view.transform.position = new Vector3(0f, 15f, 0f);
            _query.ReturnEnemy = false;
            _input.RightClick(Vector2.one);
            Assert.That(_orders.LastAction, Is.EqualTo(UnitActionId.Move));
            Assert.That(_camera.LastOrigin.y, Is.EqualTo(15f));
        }

        private sealed class FakeCamera : ICameraService
        {
            public Vector3 LastOrigin { get; private set; }
            public string Id => nameof(FakeCamera);
            public Vector3 CameraPosition => default;
            public Transform CameraTransform => null;
            public Vector3 CameraForward => Vector3.forward;
            public float FieldOfView => 60f;
            public Vector3 GetWorldPoint(Vector2 point, Vector3 origin)
            {
                LastOrigin = origin;
                return new Vector3(point.x, 0f, point.y);
            }
            public RaycastHit ScreenPointToRay(Vector2 point) => default;
            public Vector3 WorldToViewportPoint(Vector3 point) => point;
            public Vector2 WorldToScreenPoint(Vector3 point) => point;
            public IReadOnlyList<Vector3> GetGroundFootprint(Vector2 min, Vector2 max) =>
                Array.Empty<Vector3>();
            public void MoveTo(Vector3 point) { }
        }

        private sealed class FakeAbilities : IShipAbilityTargeting
        {
            public event Action TargetingChanged;
            public bool IsWaitingForTarget { get; set; }
            public IEntity Submitted { get; private set; }
            public void SubmitTarget(IEntity target) { Submitted = target; IsWaitingForTarget = false; }
            public void CancelTargeting() { IsWaitingForTarget = false; }
        }

        private sealed class FakeOrders : IUnitOrderService
        {
            public event Action<UnitOrder> OrderIssued { add { } remove { } }
            public UnitActionId? LastAction { get; set; }
            public int WaypointCount { get; private set; }
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
            public void IssueWaypointMove(IReadOnlyList<IEntity> units, IReadOnlyList<Vector3> points)
            { LastAction = UnitActionId.WaypointMove; WaypointCount = points.Count; }
            public void IssueHunt(IReadOnlyList<IEntity> units) => LastAction = UnitActionId.Hunt;
            public void IssueRetreat(IReadOnlyList<IEntity> units) => LastAction = UnitActionId.Retreat;
            public void CancelRetreat(IReadOnlyList<IEntity> units) { }
        }

        private sealed class FakeEntity : IEntity
        {
            public FakeEntity(long id, PlayerType side, Transform transform)
            { Id = id; PlayerType = side; HealthModel = new FakeHealth(transform); }
            public long Id { get; }
            public IModelObserver Model => null;
            public IHealthModelObserver HealthModel { get; }
            public PlayerType PlayerType { get; }
            public bool TryGetCommand<TCommand>(out TCommand command)
                where TCommand : IEntityCommand
            {
                if (PlayerType == PlayerType.Player &&
                    typeof(TCommand) == typeof(IWaypointMoveCommand))
                {
                    command = (TCommand)(IEntityCommand)new FakeWaypointCommand();
                    return true;
                }
                command = default;
                return false;
            }
        }

        private sealed class FakeWaypointCommand : IWaypointMoveCommand
        {
            public Vector3 WorldPosition => Vector3.zero;
            public float NavigationRadius => 1f;
            public void MoveAlong(IReadOnlyList<Vector3> waypoints) { }
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
