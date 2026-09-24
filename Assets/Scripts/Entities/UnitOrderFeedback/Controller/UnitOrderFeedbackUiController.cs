using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.UnitActions;
using EmpireAtWar.Entities.UnitActions.Model;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.UnitOrders;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.UnitOrderFeedback
{
    public sealed class UnitOrderFeedbackUiController : IUnitOrderFeedbackPresenter,
        IInitializable, ILateTickable, ILateDisposable
    {
        private readonly IUiService _uiService;
        private readonly ICameraService _cameraService;
        private readonly IEntityLocator _entityLocator;
        private readonly IUnitOrderService _orders;
        private readonly UnitActionTargetingModel _targeting;
        private IUnitOrderFeedbackUi _ui;
        private IEntity _attackTarget;
        private int _placedWaypointCount;

        public UnitOrderFeedbackUiController(IUiService uiService,
            ICameraService cameraService, IEntityLocator entityLocator,
            IUnitOrderService orders, UnitActionTargetingModel targeting)
        {
            _uiService = uiService;
            _cameraService = cameraService;
            _entityLocator = entityLocator;
            _orders = orders;
            _targeting = targeting;
        }

        public void Initialize()
        {
            _ui = (IUnitOrderFeedbackUi)_uiService.CreateUi(
                UiType.UnitOrderFeedback, _uiService.DefaultCanvasTransform);
            _ui.SetPresenter(this);
            _ui.Initialize();
            _orders.OrderIssued += HandleOrder;
            _targeting.Changed += HandleTargetingChanged;
            _entityLocator.EntityRemoved += HandleEntityRemoved;
        }

        public void LateTick()
        {
            if (_attackTarget == null) return;
            if (_attackTarget.HealthModel.IsDestroyed)
            {
                _ui.StopAttack();
                _attackTarget = null;
            }
            else _ui.SetAttackPosition(_cameraService.WorldToScreenPoint(
                _attackTarget.HealthModel.Transform.position));
        }

        public void AttackFeedbackCompleted() => _attackTarget = null;

        public void LateDispose()
        {
            _orders.OrderIssued -= HandleOrder;
            _targeting.Changed -= HandleTargetingChanged;
            _entityLocator.EntityRemoved -= HandleEntityRemoved;
            _ui.Dispose();
            _attackTarget = null;
        }

        private void HandleOrder(UnitOrder order)
        {
            if (order.Issuer != PlayerType.Player) return;
            if (order.Action == UnitActionId.Attack && order.Target != null)
            {
                _attackTarget = order.Target;
                _ui.PlayAttack(_cameraService.WorldToScreenPoint(
                    order.Target.HealthModel.Transform.position));
            }
            else if (order.Action == UnitActionId.WaypointMove &&
                     order.Waypoints != null)
            {
                foreach (Vector3 point in order.Waypoints)
                    _ui.PlayMovement(_cameraService.WorldToScreenPoint(point));
            }
            else if (order.Action == UnitActionId.Move ||
                     order.Action == UnitActionId.AttackMove ||
                     order.Action == UnitActionId.Guard ||
                     order.Action == UnitActionId.Retreat)
                _ui.PlayMovement(_cameraService.WorldToScreenPoint(order.Point));
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
                _ui.PlayMovement(_cameraService.WorldToScreenPoint(
                    new Vector3(point.X, 0f, point.Z)));
            }
            _placedWaypointCount = count;
        }
    }
}
