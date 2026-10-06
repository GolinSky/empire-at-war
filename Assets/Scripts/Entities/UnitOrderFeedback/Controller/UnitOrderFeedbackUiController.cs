using static EmpireAtWar.Utils.FormationConversion;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Components.Ship.Health.HardPointOverlay;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.UnitActions;
using EmpireAtWar.Entities.UnitActions.Model;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.UnitOrders;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using Zenject;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;

namespace EmpireAtWar.Entities.UnitOrderFeedback
{
    public sealed class UnitOrderFeedbackUiController : UiController, IUnitOrderFeedbackPresenter,
        IInitializable, ILateTickable, ILateDisposable
    {
        private readonly ILocalPlayer _localPlayer;
        private readonly ICameraService _cameraService;
        private readonly IEntityLocator _entityLocator;
        private readonly IUnitOrderService _unitOrderService;
        private readonly IShipAbilityTargeting _abilityTargeting;
        private readonly ISelectionQuery _selectionQuery;
        private readonly IHardPointHoverObserver _hardPointHover;
        private readonly IPointerInput _pointerInput;
        private IUnitOrderFeedbackUi _ui;
        private IEntity _attackTarget;

        private readonly UnitActionTargetingModel _targeting;

        private Vector3? _movementPoint;

        private int _placedWaypointCount;

        public UnitOrderFeedbackUiController(IUiService uiService,
            IUiCancelRouter cancelRouter,
            ICameraService cameraService, IEntityLocator entityLocator,
            IUnitOrderService unitOrderService, ILocalPlayer localPlayer,
            UnitActionTargetingModel targeting, IShipAbilityTargeting abilityTargeting,
            ISelectionQuery selectionQuery, IHardPointHoverObserver hardPointHover,
            IPointerInput pointerInput) : base(uiService, cancelRouter)
        {
            _localPlayer = localPlayer;
            _cameraService = cameraService;
            _entityLocator = entityLocator;
            _unitOrderService = unitOrderService;
            _targeting = targeting;
            _abilityTargeting = abilityTargeting;
            _selectionQuery = selectionQuery;
            _hardPointHover = hardPointHover;
            _pointerInput = pointerInput;
        }

        public void Initialize()
        {
            _ui = (IUnitOrderFeedbackUi)UiService.CreateUi(
                UiType.UnitOrderFeedback, UiService.DefaultCanvasTransform);
            _ui.SetPresenter(this);
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
            _ui.Dispose();
            _attackTarget = null;
            _movementPoint = null;
        }

        public void LateTick()
        {
            UpdateInvalidTarget();

            if (_movementPoint.HasValue)
                _ui.SetMovementPosition(_cameraService.WorldToScreenPoint(_movementPoint.Value));

            if (_attackTarget == null) return;
            if (_attackTarget.HealthModel.IsDestroyed)
            {
                _ui.StopAttack();
                _attackTarget = null;
            }
            else _ui.SetAttackPosition(_cameraService.WorldToScreenPoint(
                _attackTarget.GetFacade<IEntityTransformFacade>().Transform.position));
        }

        public void AttackFeedbackCompleted() => _attackTarget = null;

        public void MovementFeedbackCompleted() => _movementPoint = null;

        // Clicking an ineligible enemy keeps ability targeting armed, so the cursor marks it as invalid.
        private void UpdateInvalidTarget()
        {
            Vector2 cursor = _pointerInput.Position;
            IEntity hovered = _abilityTargeting.IsWaitingForTarget ? FindHovered(cursor) : null;
            if (hovered != null && _localPlayer.IsHostile(hovered.Owner) && !_abilityTargeting.IsValidTarget(hovered))
                _ui.ShowInvalidTarget(cursor);
            else _ui.HideInvalidTarget();
        }

        // Same lookup as PlayerOrderInputHandler: a hardpoint marker wins over the hull under it.
        private IEntity FindHovered(Vector2 cursor)
        {
            if (_hardPointHover.TryGetHovered(out IEntity ship, out int _)) return ship;
            return _selectionQuery.TryFindAt(cursor, out SelectionEntry hit) ? hit.Entity : null;
        }

        private void HandleOrder(UnitOrder order)
        {
            if (!_localPlayer.IsLocal(order.Issuer)) return;
            if (order.Action == UnitActionId.Attack && order.Target != null)
            {
                _attackTarget = order.Target;
                _ui.PlayAttack(_cameraService.WorldToScreenPoint(
                    order.Target.GetFacade<IEntityTransformFacade>().Transform.position));
            }
            else if (order.Action == UnitActionId.WaypointMove &&
                     order.Waypoints != null)
            {
                foreach (Vector3 point in order.Waypoints)
                    PlayMovement(point);
            }
            else if (order.Action == UnitActionId.Move ||
                     order.Action == UnitActionId.AttackMove ||
                     order.Action == UnitActionId.Guard ||
                     order.Action == UnitActionId.Retreat)
                PlayMovement(order.Point);
        }

        private void PlayMovement(Vector3 worldPoint)
        {
            _movementPoint = worldPoint;
            _ui.PlayMovement(_cameraService.WorldToScreenPoint(worldPoint));
        }

        private void HandleEntityRemoved(IEntity entity)
        {
            if (entity != _attackTarget) return;
            _ui.StopAttack();
            _attackTarget = null;
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
