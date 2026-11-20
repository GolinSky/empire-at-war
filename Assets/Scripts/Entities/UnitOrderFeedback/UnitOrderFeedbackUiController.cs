using static EmpireAtWar.Utils.FormationConversion;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.UnitActions;
using EmpireAtWar.Entities.UnitActions.Model;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.UnitOrders;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Entities.UnitOrderFeedback
{
    public sealed class UnitOrderFeedbackUiController : UiController,
        IInitializable, ILateTickable, ILateDisposable
    {
        private readonly ILocalPlayer _localPlayer;
        private readonly ICameraService _cameraService;
        private readonly IEntityLocator _entityLocator;
        private readonly IUnitOrderService _unitOrderService;
        private readonly IOrderCursorService _orderCursor;
        private readonly IPointerInput _pointerInput;
        private UnitOrderFeedbackUi _ui;
        private IEntity _feedbackTarget;

        private readonly UnitActionTargetingModel _targeting;

        private Vector3? _movementPoint;

        private int _placedWaypointCount;

        public UnitOrderFeedbackUiController(IUiService uiService,
            IUiCancelRouter cancelRouter,
            ICameraService cameraService, IEntityLocator entityLocator,
            IUnitOrderService unitOrderService, ILocalPlayer localPlayer,
            UnitActionTargetingModel targeting, IOrderCursorService orderCursor,
            IPointerInput pointerInput) : base(uiService, cancelRouter)
        {
            _localPlayer = localPlayer;
            _cameraService = cameraService;
            _entityLocator = entityLocator;
            _unitOrderService = unitOrderService;
            _targeting = targeting;
            _orderCursor = orderCursor;
            _pointerInput = pointerInput;
        }

        public void Initialize()
        {
            _ui = CreateUi<UnitOrderFeedbackUi>();
            _ui.Initialize();
            _unitOrderService.OrderIssued += HandleOrder;
            _targeting.Changed += HandleTargetingChanged;
            _entityLocator.EntityRemoved += HandleEntityRemoved;
        }

        public void LateDispose()
        {
            _unitOrderService.OrderIssued -= HandleOrder;
            _targeting.Changed -= HandleTargetingChanged;
            _entityLocator.EntityRemoved -= HandleEntityRemoved;
            if (_ui != null) _ui.Dispose();
            _feedbackTarget = null;
            _movementPoint = null;
        }

        public void LateTick()
        {
            _ui.SetCursor(_orderCursor.Resolve(), _pointerInput.Position);

            if (_movementPoint.HasValue)
                _ui.SetMovementPosition(_cameraService.WorldToScreenPoint(_movementPoint.Value));

            if (_feedbackTarget == null) return;
            if (_feedbackTarget.HealthModel.IsDestroyed)
            {
                _ui.StopTargetOrder();
                _feedbackTarget = null;
                return;
            }

            _ui.SetTargetOrderPosition(_cameraService.WorldToScreenPoint(
                _feedbackTarget.GetFacade<IEntityTransformFacade>().Transform.position));
        }

        private void TargetFeedbackCompleted() => _feedbackTarget = null;

        private void MovementFeedbackCompleted() => _movementPoint = null;

        private void HandleOrder(UnitOrder order)
        {
            if (!_localPlayer.IsLocal(order.Issuer)) return;
            bool isGuard = order.Action == UnitActionId.Guard;
            if ((order.Action == UnitActionId.Attack || isGuard) && order.Target != null)
            {
                _feedbackTarget = order.Target;
                _ui.PlayTargetOrder(_cameraService.WorldToScreenPoint(
                    _feedbackTarget.GetFacade<IEntityTransformFacade>().Transform.position), isGuard,
                    TargetFeedbackCompleted);
            }
            else if (order.Action == UnitActionId.WaypointMove &&
                     order.Waypoints != null)
            {
                foreach (Vector3 point in order.Waypoints)
                    PlayMovement(point);
            }
            else if (order.Action == UnitActionId.Move ||
                     order.Action == UnitActionId.AttackMove ||
                     order.Action == UnitActionId.Retreat)
                PlayMovement(order.Point, order.Action == UnitActionId.Move && order.IsLoose);
        }

        private void PlayMovement(Vector3 worldPoint, bool isLoose = false)
        {
            _movementPoint = worldPoint;
            _ui.PlayMovement(_cameraService.WorldToScreenPoint(worldPoint), isLoose, MovementFeedbackCompleted);
        }

        private void HandleEntityRemoved(IEntity entity)
        {
            if (entity != _feedbackTarget) return;
            _ui.StopTargetOrder();
            _feedbackTarget = null;
        }

        private void HandleTargetingChanged()
        {
            if (_targeting.Pending != UnitActionId.WaypointMove)
            {
                _placedWaypointCount = 0;
                return;
            }

            int count = _targeting.Waypoints.Count;
            if (count > _placedWaypointCount)
            {
                var point = _targeting.Waypoints[count - 1];
                PlayMovement(ToVector(point));
            }
            _placedWaypointCount = count;
        }
    }
}
