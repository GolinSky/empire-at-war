using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Models.SkirmishGame;
using EmpireAtWar.Entities.UnitActions.Model;
using EmpireAtWar.Entities.UnitActions.Ui;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.InputService;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.UnitOrders;
using Zenject;

namespace EmpireAtWar.Entities.UnitActions.Controller
{
    public sealed class UnitActionsPresenter : IInitializable, ILateDisposable,
        ITickable, IObserver<ISelectionSubject>
    {
        private readonly IUnitActionsViewProvider _coreUi;
        private readonly ISelectionService _selection;
        private readonly IInputService _input;
        private readonly IShipAbilityTargeting _abilities;
        private readonly UnitActionTargetingModel _targeting;
        private readonly IPlayerOrderInputHandler _inputHandler;
        private readonly IUnitOrderService _orders;
        private readonly ISkirmishSessionModelObserver _session;
        private readonly Dictionary<UnitActionId, bool> _availability =
            new Dictionary<UnitActionId, bool>();
        private IUnitActionsView _view;
        private bool _battleEnded;

        public UnitActionsPresenter(IUnitActionsViewProvider coreUi,
            ISelectionService selection, IInputService input,
            IShipAbilityTargeting abilities, UnitActionTargetingModel targeting,
            IPlayerOrderInputHandler inputHandler, IUnitOrderService orders,
            ISkirmishSessionModelObserver session)
        {
            _coreUi = coreUi;
            _selection = selection;
            _input = input;
            _abilities = abilities;
            _targeting = targeting;
            _inputHandler = inputHandler;
            _orders = orders;
            _session = session;
        }

        public void Initialize()
        {
            _view = _coreUi.UnitActionsView;
            _view.Initialize();
            _view.ActionPressed += HandleAction;
            _selection.AddObserver(this);
            _input.OnEscapePressed += Cancel;
            _abilities.TargetingChanged += HandleAbilityTargeting;
            _targeting.Changed += RefreshPending;
            RefreshAvailability();
        }

        public void LateDispose()
        {
            _view.ActionPressed -= HandleAction;
            _selection.RemoveObserver(this);
            _input.OnEscapePressed -= Cancel;
            _abilities.TargetingChanged -= HandleAbilityTargeting;
            _targeting.Changed -= RefreshPending;
            _view.Dispose();
            _targeting.Cancel();
        }

        public void UpdateState(ISelectionSubject subject)
        {
            if (subject.UpdatedType != PlayerType.Player) return;
            _targeting.Cancel();
            RefreshAvailability();
        }

        public void Tick()
        {
            if (_battleEnded != _session.IsBattleEnded)
            {
                _battleEnded = _session.IsBattleEnded;
                if (_battleEnded) _targeting.Cancel();
                RefreshAvailability();
            }
        }

        private void HandleAction(UnitActionId action)
        {
            if (!_availability.TryGetValue(action, out bool available) || !available) return;
            if (action == UnitActionId.Stop)
            {
                _targeting.Cancel();
                _orders.IssueStop(Snapshot());
                return;
            }

            if (action == UnitActionId.Hunt)
            {
                _targeting.Cancel();
                _orders.IssueHunt(Snapshot());
                return;
            }

            if (action == UnitActionId.Retreat)
            {
                _targeting.Cancel();
                _orders.IssueRetreat(Snapshot());
                return;
            }

            if (_targeting.Pending == action)
            {
                if (action == UnitActionId.WaypointMove)
                    _inputHandler.FinishWaypoints();
                else _targeting.Cancel();
                return;
            }

            _abilities.CancelTargeting();
            _targeting.Start(action);
        }

        private void RefreshAvailability()
        {
            bool any = false;
            foreach (UnitActionId action in Enum.GetValues(typeof(UnitActionId)))
            {
                bool available = !_session.IsBattleEnded && HasReceiver(action);
                _availability[action] = available;
                _view.SetAvailable(action, available);
                any |= available;
            }
            _view.SetVisible(any);
        }

        private bool HasReceiver(UnitActionId action)
        {
            foreach (IEntity entity in _selection.PlayerSelectionContext.Entities)
            {
                if (entity.HealthModel.IsDestroyed || !entity.HealthModel.HasUnits) continue;
                // Move has no panel button: moving is the default right-click order.
                switch (action)
                {
                    case UnitActionId.Attack:
                        if (entity.TryGetFacade(out IAttackFacade _) ||
                            entity.TryGetFacade(out IFocusFireFacade _)) return true;
                        break;
                    case UnitActionId.AttackMove:
                        if (entity.TryGetFacade(out IAttackMoveFacade _)) return true;
                        break;
                    case UnitActionId.Stop:
                        if (entity.TryGetFacade(out IStopFacade _)) return true;
                        break;
                    case UnitActionId.Guard:
                        if (entity.TryGetFacade(out IGuardFacade _)) return true;
                        break;
                    case UnitActionId.WaypointMove:
                        if (entity.TryGetFacade(out IWaypointMoveFacade _)) return true;
                        break;
                    case UnitActionId.Hunt:
                        if (entity.TryGetFacade(out IHuntFacade _)) return true;
                        break;
                    case UnitActionId.Retreat:
                        if (entity.TryGetFacade(out IRetreatFacade _)) return true;
                        break;
                }
            }
            return false;
        }

        private List<IEntity> Snapshot()
        {
            List<IEntity> receivers = new List<IEntity>();
            foreach (IEntity entity in _selection.PlayerSelectionContext.Entities)
                if (!entity.HealthModel.IsDestroyed && entity.HealthModel.HasUnits)
                    receivers.Add(entity);
            return receivers;
        }

        private void HandleAbilityTargeting()
        {
            if (_abilities.IsWaitingForTarget) _targeting.Cancel();
        }

        private void RefreshPending() => _view.SetPending(_targeting.Pending);
        private void Cancel() => _targeting.Cancel();
    }
}
