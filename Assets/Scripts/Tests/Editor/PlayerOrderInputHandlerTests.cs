using System;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using EmpireAtWar.Components.Selection.Marquee;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Health.HardPointOverlay;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.MiniMap;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Entities.UnitActions;
using EmpireAtWar.Entities.UnitActions.Model;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.Layer;
using EmpireAtWar.Services.Selection;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.UnitOrders;
using NUnit.Framework;
using UnityEngine;

namespace EmpireAtWar.Tests.Editor
{
    [Category("BattleInput")]
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
        private FakeCaptureSite _captureSite;
        private PlayerOrderInputHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _view = new GameObject("Test entity");
            _receiver = new FakeEntity(id: 1, side: TestPlayers.Human, transform: _view.transform);
            _enemy = new FakeEntity(id: 2, side: TestPlayers.Enemy, transform: _view.transform);
            _input = new FakeInput();
            _selection = new FakeSelection(_receiver);
            _query = new FakeQuery(_enemy);
            _abilities = new FakeAbilities();
            _targeting = new UnitActionTargetingModel();
            _orders = new FakeOrders();
            _camera = new FakeCamera();
            _captureSite = new FakeCaptureSite();
            _handler = new PlayerOrderInputHandler(gestures: _input, orderInput: _input, selectionService: _selection, query: _query,
                cameraService: _camera, layerService: null, abilities: _abilities, targeting: _targeting, unitOrderService: _orders, superWeapons: new SuperWeaponTargetingModel(),
                hardPointHover: new HardPointOverlayModel(),
                localPlayer: TestPlayers.CreateLocalPlayer(TestPlayers.CreateDuel()), captureSites: _captureSite);
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
            _input.IsQueueWaypointHeld = true;
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

        [Test]
        public void DefaultMove_ProjectsClickOntoReceiverHeight()
        {
            _view.transform.position = new Vector3(0f, 15f, 0f);
            _query.ReturnEnemy = false;
            _input.RightClick(Vector2.one);
            Assert.That(_orders.LastAction, Is.EqualTo(UnitActionId.Move));
            Assert.That(_camera.LastOrigin.y, Is.EqualTo(15f));
        }

        [Test]
        public void MinimapPoint_HonorsAttackMoveAndWaypointModes()
        {
            _targeting.Start(UnitActionId.AttackMove);
            Assert.That(_handler.TryIssueMove(Vector3.one), Is.True);
            Assert.That(_orders.LastAction, Is.EqualTo(UnitActionId.AttackMove));
            Assert.That(_targeting.Pending, Is.Null);
            _orders.LastAction = null;
            _targeting.Start(UnitActionId.WaypointMove);
            Assert.That(_handler.TryIssueMove(Vector3.one), Is.True);
            Assert.That(_targeting.Waypoints.Count, Is.EqualTo(1));
            Assert.That(_orders.LastAction, Is.Null);
        }

        [TestCase(UnitActionId.Attack)]
        [TestCase(UnitActionId.Guard)]
        public void MinimapPoint_DoesNotIssueMoveForEntityTargeting(UnitActionId action)
        {
            _targeting.Start(action);
            Assert.That(_handler.TryIssueMove(Vector3.one), Is.False);
            Assert.That(_targeting.Pending, Is.EqualTo(action));
            Assert.That(_orders.LastAction, Is.Null);
            _targeting.Cancel();
            _abilities.IsWaitingForTarget = true;
            Assert.That(_handler.TryIssueMove(Vector3.one), Is.False);
            Assert.That(_abilities.IsWaitingForTarget, Is.True);
            Assert.That(_orders.LastAction, Is.Null);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CaptureSiteCommand_MovesToSiteCenterAtReceiverHeight(bool minimap)
        {
            _captureSite.Enabled = true;
            _query.ReturnEnemy = false;
            _view.transform.position = new Vector3(0f, 15f, 0f);

            if (minimap)
                Assert.That(_handler.TryIssueMove(new Vector3(11f, 0f, 21f)), Is.True);
            else
                _input.RightClick(new Vector2(11f, 21f));

            Assert.That(_orders.LastAction, Is.EqualTo(UnitActionId.Move));
            Assert.That(_orders.LastPoint, Is.EqualTo(new Vector3(10f, 15f, 20f)));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CaptureSiteCommand_PreservesAttackMoveMode(bool minimap)
        {
            _captureSite.Enabled = true;
            _query.ReturnEnemy = false;
            _targeting.Start(UnitActionId.AttackMove);

            if (minimap)
                Assert.That(_handler.TryIssueMove(new Vector3(11f, 0f, 21f)), Is.True);
            else
                _input.RightClick(new Vector2(11f, 21f));

            Assert.That(_orders.LastAction, Is.EqualTo(UnitActionId.AttackMove));
            Assert.That(_orders.LastPoint, Is.EqualTo(new Vector3(10f, 0f, 20f)));
            Assert.That(_targeting.Pending, Is.Null);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CaptureSiteCommand_DoesNotReplacePendingAttack(bool minimap)
        {
            _captureSite.Enabled = true;
            _query.ReturnEnemy = false;
            _targeting.Start(UnitActionId.Attack);

            if (minimap)
                Assert.That(_handler.TryIssueMove(new Vector3(11f, 0f, 21f)), Is.False);
            else
                _input.RightClick(new Vector2(11f, 21f));

            Assert.That(_orders.LastAction, Is.Null);
            Assert.That(_targeting.Pending, Is.EqualTo(UnitActionId.Attack));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CaptureSiteCommand_QueuesWaypointWithoutIssuingImmediateMove(bool minimap)
        {
            _captureSite.Enabled = true;
            _query.ReturnEnemy = false;
            _targeting.Start(UnitActionId.WaypointMove);

            if (minimap)
                Assert.That(_handler.TryIssueMove(new Vector3(11f, 0f, 21f)), Is.True);
            else
                _input.RightClick(new Vector2(11f, 21f));

            Assert.That(_orders.LastAction, Is.Null);
            Assert.That(_targeting.Waypoints.Count, Is.EqualTo(1));
            _handler.FinishWaypoints();
            Assert.That(_orders.LastAction, Is.EqualTo(UnitActionId.WaypointMove));
            Assert.That(_orders.LastPoint, Is.EqualTo(new Vector3(10f, 0f, 20f)));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void WorldCaptureSiteCommand_FriendlyFacilityMovesAndEnemyFacilityAttacks(bool enemy)
        {
            _captureSite.Enabled = true;
            _query.Target = enemy ? _enemy : _receiver;
            _query.Command = new FakeFacilitySelection();

            _input.RightClick(new Vector2(11f, 21f));

            Assert.That(_orders.LastAction, Is.EqualTo(enemy ? UnitActionId.Attack : UnitActionId.Move));
            if (!enemy)
                Assert.That(_orders.LastPoint, Is.EqualTo(new Vector3(10f, 0f, 20f)));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MinimapObstacle_ForwardsOnlyCaptureSiteCommands(bool insideSite)
        {
            _captureSite.Enabled = insideSite;
            MiniMapData model = ScriptableObject.CreateInstance<MiniMapData>();
            try
            {
                model.AddObstacle(new MiniMapObstacle(10f, 20f, 4f, 4f));
                var orderInput = new RecordingOrderInput();
                var controller = new MiniMapUiController(battleMap: null, cameraService: null,
                    inputLock: new UnlockedInput(), uiService: null, cancelRouter: null,
                    routeNavigation: null, orderInput: orderInput, tooltipService: null,
                    fogMask: null, model: model, playerColors: null, timerPoolService: null,
                    captureSites: _captureSite);

                controller.OrderMove(new Vector3(11f, 0f, 21f));

                Assert.That(orderInput.WasCalled, Is.EqualTo(insideSite));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(model);
            }
        }

        private sealed class FakeFacilitySelection : IEntitySelectionFacade
        {
            public SelectionType SelectionType { get; set; } = SelectionType.MiningFacility;
            public void Select(bool isSelected) { }
        }

        private sealed class RecordingOrderInput : IPlayerOrderInputHandler
        {
            public bool WasCalled { get; private set; }
            public void FinishWaypoints() { }
            public bool TryIssueMove(Vector3 worldPoint)
            { WasCalled = true; return false; }
        }

        private sealed class UnlockedInput : IInputLock
        {
            public event Action<bool> LockChanged { add { } remove { } }
            public bool IsLocked => false;
            public IDisposable Acquire() => throw new NotSupportedException();
            public IDisposable AcquireBattle() => throw new NotSupportedException();
        }

        private sealed class FakeCaptureSite : ICaptureSite, ICaptureSitesSystem
        {
            public CaptureSiteKind Kind => CaptureSiteKind.Mining;
            public int PointId => -1;

            public event System.Action<PlayerId, PlayerId, SiteFacilityType> SiteCaptured { add { } remove { } }
            public event System.Action<PlayerId, SiteFacilityType> SiteLost { add { } remove { } }
            public event System.Action<PlayerId, SiteFacilityType> ConstructionStarted { add { } remove { } }
            public bool Enabled { get; set; }
            public PlayerId Owner => PlayerId.None;
            public CaptureSiteState State => CaptureSiteState.Neutral;
            public SiteFacilityType FacilityType => SiteFacilityType.Mining;
            public float CaptureProgress => 0f;
            public float ConstructionProgress => 0f;
            public Vector3 Center => new Vector3(10f, 3f, 20f);
            public bool IsRevealed => true;
            public bool IsOperational => false;
            public IReadOnlyList<ICaptureSite> Sites => Enabled ? new ICaptureSite[] { this } : Array.Empty<ICaptureSite>();

            public bool Contains(Vector3 position, float clearance = 0f) =>
                Vector2.Distance(new Vector2(position.x, position.z), new Vector2(Center.x, Center.z)) <= 5f + clearance;

            public bool IsPositionInAnySite(Vector3 position, float clearance = 0f) => Enabled && Contains(position, clearance);
            public bool TryGetCaptureTarget(PlayerId owner, Vector3 origin, out Vector3 position)
            { position = default; return false; }
            public bool TryGetThreatenedSite(PlayerId owner, out Vector3 position)
            { position = default; return false; }
            public bool TryGetRaidTarget(PlayerId attacker, Vector3 origin, out Vector3 position)
            { position = default; return false; }
            public bool HasHostileUnits(ICaptureSite site, PlayerId owner) => false;
            public bool TryStartConstruction(int siteIndex, PlayerId owner, SiteFacilityType type) => false;
            public bool TryBuildOnOwnedSite(PlayerId owner) => false;
        }

        private sealed class FakeInput : IPointerGestures, IUnitOrderInput
        {
            public event Action<Vector2> WorldPressed { add { } remove { } }

            public event Action<Vector2> WorldClicked { add { } remove { } }

            public event Action<Vector2> WorldCommanded;

            public event Action<Vector2> DragStarted { add { } remove { } }

            public event Action<Vector2> DragChanged { add { } remove { } }

            public event Action<Vector2> DragEnded { add { } remove { } }
            public event Action DragCanceled { add { } remove { } }

            public event Action QueueWaypointReleased;

            public bool IsQueueWaypointHeld { get; set; }

            public void RightClick(Vector2 point) => WorldCommanded?.Invoke(point);

            public void ReleaseAlt()
            { IsQueueWaypointHeld = false; QueueWaypointReleased?.Invoke(); }
        }

        private sealed class FakeSelection : ISelectionService, ISelectionContext
        {
            public string Id => nameof(FakeSelection);
            public IEntity Entity { get; }
            public IReadOnlyList<IEntity> Entities => new[] { Entity };
            public IEntitySelectionFacade SelectionFacade => null;
            public SelectionType SelectionType => SelectionType.Ship;
            public bool HasSelectable => true;
            public int Count => 1;
            public SelectionScope Scope => SelectionScope.Local;
            public ISelectionContext PlayerSelectionContext => this;
            public ISelectionContext OtherSelectionContext => null;

            public FakeSelection(IEntity entity) { Entity = entity; }

            public bool Contains(IEntity entity) => entity.Id == Entity.Id;

            public void RemoveSelectable(ISelectionContext context) { }

            public void SelectCurrentUnitsByType(EmpireAtWar.Entities.Units.UnitTypeId unitTypeId) { }

            public void AddObserver(IObserver<ISelectionSubject> observer) { }

            public void RemoveObserver(IObserver<ISelectionSubject> observer) { }
        }

        private sealed class FakeQuery : ISelectionQuery
        {
            public IEntity Target { get; set; }
            public IEntitySelectionFacade Command { get; set; }

            public bool ReturnEnemy { get; set; } = true;

            public FakeQuery(IEntity enemy) { Target = enemy; }

            public bool TryFindAt(Vector2 point, out SelectionEntry result)
            {
                result = ReturnEnemy ? new SelectionEntry(Target, Command) : default;
                return ReturnEnemy;
            }

            public void CollectAt(Vector2 point, IList<SelectionEntry> result)
            {
                if (ReturnEnemy)
                {
                    result.Add(new SelectionEntry(Target, Command));
                }
            }

            public void CollectSameShipType(SelectionEntry entry, ICollection<SelectionEntry> result) { }

            public void CollectAllPlayerUnits(ICollection<SelectionEntry> result) { }

            public void CollectVisiblePlayerUnits(ICollection<SelectionEntry> result) { }

            public void CollectInside(MarqueeRectangle rect, ICollection<SelectionEntry> result) { }
        }

        private sealed class FakeCamera : ICameraService
        {
            public Vector3 LastOrigin { get; private set; }
            public string Id => nameof(FakeCamera);
            public Vector3 CameraPosition => default;
            public Transform CameraTransform => null;
            public Vector3 CameraForward => Vector3.forward;
            public float FieldOfView => 60f;
            public UnityEngine.Camera SceneCamera => null;
            public float ZoomTargetHeight => 0f;

            public Vector3 GetWorldPoint(Vector2 point, Vector3 origin)
            {
                LastOrigin = origin;
                return new Vector3(point.x, 0f, point.y);
            }

            public RaycastHit ScreenPointToRay(Vector2 point) => default;
            public int ScreenPointRaycastAll(Vector2 point, RaycastHit[] results) => 0;

            public Vector3 WorldToViewportPoint(Vector3 point) => point;

            public Vector2 WorldToScreenPoint(Vector3 point) => point;

            public IReadOnlyList<Vector3> GetGroundFootprint(Vector2 min, Vector2 max) =>
                Array.Empty<Vector3>();

            public void MoveTo(Vector3 point) { }

            public void SetPose(Vector3 position, Quaternion rotation) { }
            public void RestorePose(Vector3 position, Quaternion rotation, float zoomTargetHeight) { }

            public void SetCullingMask(LayerMask mask) { }
        }

        private sealed class FakeAbilities : IShipAbilityTargeting
        {
            public event Action TargetingChanged { add { } remove { } }

            public bool IsWaitingForTarget { get; set; }
            public IEntity Submitted { get; private set; }

            public bool IsValidTarget(IEntity target) => true;

            public void SubmitTarget(IEntity target) { Submitted = target; IsWaitingForTarget = false; }

            public void CancelTargeting() { IsWaitingForTarget = false; }
        }

        private sealed class FakeOrders : IUnitOrderService
        {
            public event Action<UnitOrder> OrderIssued { add { } remove { } }

            public UnitActionId? LastAction { get; set; }
            public Vector3 LastPoint { get; private set; }
            public int WaypointCount { get; private set; }

            public void IssueMove(IReadOnlyList<IEntity> units, Vector3 point)
            { LastAction = UnitActionId.Move; LastPoint = point; }

            public void IssueMove(IReadOnlyList<IEntity> units, IReadOnlyList<Vector3> points) => LastAction = UnitActionId.Move;

            public void IssueAttack(IReadOnlyList<IEntity> units, IEntity target) => LastAction = UnitActionId.Attack;

            public void IssueAttack(IReadOnlyList<IEntity> units, IEntity target,
                IReadOnlyList<Vector3> offsets) => LastAction = UnitActionId.Attack;

            public void IssueHardPointAttack(IReadOnlyList<IEntity> units, IEntity target,
                int hardPointId) => LastAction = UnitActionId.Attack;

            public void IssueAttackMove(IReadOnlyList<IEntity> units, Vector3 point)
            { LastAction = UnitActionId.AttackMove; LastPoint = point; }

            public void IssueStop(IReadOnlyList<IEntity> units) => LastAction = UnitActionId.Stop;

            public void IssueGuard(IReadOnlyList<IEntity> units, IEntity target) => LastAction = UnitActionId.Guard;

            public void IssueGuard(IReadOnlyList<IEntity> units, IEntity target,
                IReadOnlyList<Vector3> offsets) => LastAction = UnitActionId.Guard;

            public void IssueWaypointMove(IReadOnlyList<IEntity> units, IReadOnlyList<Vector3> points)
            { LastAction = UnitActionId.WaypointMove; WaypointCount = points.Count; LastPoint = points[0]; }

            public void IssueHunt(IReadOnlyList<IEntity> units) => LastAction = UnitActionId.Hunt;

            public void IssueRetreat(IReadOnlyList<IEntity> units) => LastAction = UnitActionId.Retreat;
        }

        private sealed class FakeEntity : IEntity
        {
            public long Id { get; }
            public IHealthModelObserver HealthModel { get; }
            public PlayerId Owner { get; }

            public FakeEntity(Transform transform, PlayerId side, long id)
            { Id = id; Owner = side; HealthModel = new FakeHealth(transform); }

            public TCommand GetFacade<TCommand>() where TCommand : IEntityFacade
            { TryGetFacade(out TCommand facade); return facade; }

            public bool TryGetFacade<TCommand>(out TCommand command)
                where TCommand : IEntityFacade
            { if (HealthModel is TCommand transformFacade) { command = transformFacade; return true; }
                if (Owner == TestPlayers.Human &&
                    (typeof(TCommand) == typeof(IWaypointMoveFacade) || typeof(TCommand) == typeof(IMoveFacade)))
                {
                    command = (TCommand)(IEntityFacade)new FakeWaypointCommand(
                        ((IEntityTransformFacade)HealthModel).Transform);
                    return true;
                }
                command = default;
                return false;
            }
        }

        private sealed class FakeWaypointCommand : IWaypointMoveFacade, IMoveFacade
        {
            private readonly Transform _transform;
            public Vector3 WorldPosition => _transform.position;
            public float NavigationRadius => 1f;

            public FakeWaypointCommand(Transform transform) => _transform = transform;
            public void MoveTo(Vector2 screenPosition) { }
            public void MoveTo(Vector3 worldPosition) { }
            public void MoveAlong(IReadOnlyList<Vector3> waypoints) { }
        }

        private sealed class FakeHealth : IHealthModelObserver, IEntityTransformFacade
        {
            public event Action OnDestroy { add { } remove { } }

            public event Action OnValueChanged { add { } remove { } }

            public HardPointModel[] HardPointModels => Array.Empty<HardPointModel>();
            public float Hull => 1f;
            public ShipClass ShipClass => ShipClass.Capital;
            public bool HasLiveHardPoints => true;
            public float HullPercentage => 1f;
            public float Shields => 1f;
            public float ShieldPercentage => 1f;
            public bool IsDestroyed => false;
            public bool IsLostShieldGenerator => false;
            public bool HasUnits => true;
            public PlayerId Owner => TestPlayers.Human;
            public Transform Transform { get; }
            public bool HasShields => true;

            public FakeHealth(Transform transform) { Transform = transform; }

            public IHardPointModel[] GetShipUnits(HardPointType type) =>
                Array.Empty<IHardPointModel>();
        }
    }
}
