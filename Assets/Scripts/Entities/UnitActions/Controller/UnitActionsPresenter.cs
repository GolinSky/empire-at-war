using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
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
            float? remaining = null;
            foreach (IEntity entity in _selection.PlayerSelectionContext.Entities)
                if (!entity.HealthModel.IsDestroyed &&
                    entity.TryGetCommand(out IRetreatCommand command) &&
                    command.IsRetreatPending &&
                    (!remaining.HasValue || command.RetreatRemaining < remaining.Value))
                    remaining = command.RetreatRemaining;
            _view.SetRetreatCountdown(remaining);
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
                List<IEntity> receivers = Snapshot();
                bool allPending = true;
                bool any = false;
                foreach (IEntity entity in receivers)
                {
                    if (!entity.TryGetCommand(out IRetreatCommand command)) continue;
                    any = true;
                    allPending &= command.IsRetreatPending;
                }
                if (any && allPending) _orders.CancelRetreat(receivers);
                else _orders.IssueRetreat(receivers);
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
                switch (action)
                {
                    case UnitActionId.Move:
                        if (entity.TryGetCommand(out IMoveCommand _)) return true;
                        break;
                    case UnitActionId.Attack:
                        if (entity.TryGetCommand(out IAttackCommand _) ||
                            entity.TryGetCommand(out IFocusFireCommand _)) return true;
                        break;
                    case UnitActionId.AttackMove:
                        if (entity.TryGetCommand(out IAttackMoveCommand _)) return true;
                        break;
                    case UnitActionId.Stop:
                        if (entity.TryGetCommand(out IStopCommand _)) return true;
                        break;
                    case UnitActionId.Guard:
                        if (entity.TryGetCommand(out IGuardCommand _)) return true;
                        break;
                    case UnitActionId.WaypointMove:
                        if (entity.TryGetCommand(out IWaypointMoveCommand _)) return true;
                        break;
                    case UnitActionId.Hunt:
                        if (entity.TryGetCommand(out IHuntCommand _)) return true;
                        break;
                    case UnitActionId.Retreat:
                        if (entity.TryGetCommand(out IRetreatCommand _)) return true;
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
