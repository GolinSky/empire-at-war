using static EmpireAtWar.Utils.FormationConversion;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Entities.UnitActions;
using EmpireAtWar.Entities.UnitActions.Model;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.ShipAbilities;
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
        private readonly IShipAbilityTargeting _abilities;
        private readonly SuperWeaponTargetingModel _superWeapons;
        private readonly ShipAbilityCatalog _abilityCatalog;
        private UnitOrderFeedbackUi _ui;
        private IEntity _feedbackTarget;
        // Attack-move marks a point, not an entity.
        private Vector3? _feedbackPoint;

        private readonly UnitActionTargetingModel _targeting;

        private Vector3? _movementPoint;

        private int _placedWaypointCount;

        public UnitOrderFeedbackUiController(IUiService uiService,
            IUiCancelRouter cancelRouter,
            ICameraService cameraService, IEntityLocator entityLocator,
            IUnitOrderService unitOrderService, ILocalPlayer localPlayer,
            UnitActionTargetingModel targeting, IOrderCursorService orderCursor,
            IPointerInput pointerInput, IShipAbilityTargeting abilities,
            SuperWeaponTargetingModel superWeapons, ShipAbilityCatalog abilityCatalog) : base(uiService, cancelRouter)
        {
            _abilities = abilities;
            _superWeapons = superWeapons;
            _abilityCatalog = abilityCatalog;
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
            _abilities.TargetAccepted += HandleAbilityTargetAccepted;
            _superWeapons.TargetAccepted += HandleSuperWeaponTargetAccepted;
            _entityLocator.EntityRemoved += HandleEntityRemoved;
        }

        public void LateDispose()
        {
            _unitOrderService.OrderIssued -= HandleOrder;
            _targeting.Changed -= HandleTargetingChanged;
            _abilities.TargetAccepted -= HandleAbilityTargetAccepted;
            _superWeapons.TargetAccepted -= HandleSuperWeaponTargetAccepted;
            _entityLocator.EntityRemoved -= HandleEntityRemoved;
            if (_ui != null) _ui.Dispose();
            _feedbackTarget = null;
            _feedbackPoint = null;
            _movementPoint = null;
        }

        public void LateTick()
        {
            OrderCursor cursor = _orderCursor.Resolve();
            _ui.SetCursor(cursor, _pointerInput.Position, GetCursorIcon(cursor));

            if (_movementPoint.HasValue)
                _ui.SetMovementPosition(_cameraService.WorldToScreenPoint(_movementPoint.Value));

            if (_feedbackPoint.HasValue)
                _ui.SetTargetOrderPosition(_cameraService.WorldToScreenPoint(_feedbackPoint.Value));

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

        private Sprite GetCursorIcon(OrderCursor cursor) => cursor switch
        {
            OrderCursor.AbilityTarget => _abilityCatalog.Get(_abilities.PendingAbilityId).Icon,
            OrderCursor.SuperWeapon => _ui.GetSuperWeaponIcon(_superWeapons.Pending.Value),
            _ => null
        };

        private void TargetFeedbackCompleted()
        {
            _feedbackTarget = null;
            _feedbackPoint = null;
        }

        private void PlayTarget(IEntity target, OrderCursor action, Sprite actionIcon = null)
        {
            _feedbackTarget = target;
            _feedbackPoint = null;
            _ui.PlayTargetOrder(_cameraService.WorldToScreenPoint(
                target.GetFacade<IEntityTransformFacade>().Transform.position), action, actionIcon,
                TargetFeedbackCompleted);
        }

        private void HandleAbilityTargetAccepted(ShipAbilityId id, IEntity target) =>
            PlayTarget(target, OrderCursor.AbilityTarget, _abilityCatalog.Get(id).Icon);

        private void HandleSuperWeaponTargetAccepted(SuperWeaponType type, IEntity target) =>
            PlayTarget(target, OrderCursor.SuperWeapon, _ui.GetSuperWeaponIcon(type));

        private void MovementFeedbackCompleted() => _movementPoint = null;

        private void HandleOrder(UnitOrder order)
        {
            if (!_localPlayer.IsLocal(order.Issuer)) return;
            bool isGuard = order.Action == UnitActionId.Guard;
            if ((order.Action == UnitActionId.Attack || isGuard) && order.Target != null)
                PlayTarget(order.Target, isGuard ? OrderCursor.Guard : OrderCursor.Attack);
            else if (order.Action == UnitActionId.WaypointMove &&
                     order.Waypoints != null)
            {
                foreach (Vector3 point in order.Waypoints)
                    PlayMovement(point);
            }
            else if (order.Action == UnitActionId.Move ||
                     order.Action == UnitActionId.AttackMove ||
                     order.Action == UnitActionId.Retreat)
            {
                PlayMovement(order.Point, order.Action == UnitActionId.Move && order.IsLoose);
                if (order.Action == UnitActionId.AttackMove) PlayAttackMovePoint(order.Point);
            }
        }

        private void PlayAttackMovePoint(Vector3 worldPoint)
        {
            _feedbackTarget = null;
            _feedbackPoint = worldPoint;
            _ui.PlayTargetOrder(_cameraService.WorldToScreenPoint(worldPoint), OrderCursor.AttackMove, null,
                TargetFeedbackCompleted);
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
