using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Services.Tooltip;
using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Models.SkirmishGame;
using EmpireAtWar.Entities.UnitActions.Model;
using EmpireAtWar.Entities.UnitActions.Ui;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.UnitOrders;
using EmpireAtWar.Ui.Base;
using Zenject;

namespace EmpireAtWar.Entities.UnitActions.Controller
{
    public sealed class UnitActionsPresenter : UiController, IInitializable, ILateDisposable,
        ITickable, IObserver<ISelectionSubject>
    {
        private readonly IUnitActionsViewProvider _coreUi;
        private readonly ISelectionService _selection;
        private readonly IShipAbilityTargeting _abilities;
        private readonly UnitActionTargetingModel _targeting;
        private readonly IPlayerOrderInputHandler _inputHandler;
        private readonly IUnitOrderService _orders;
        private readonly ISkirmishSessionModelObserver _session;
        private readonly Dictionary<UnitActionId, bool> _availability =
            new Dictionary<UnitActionId, bool>();
        private IUnitActionsView _view;
        private bool _battleEnded;
        private readonly TooltipRequests _tooltips;
        private readonly EmpireAtWar.Services.Input.IInputBindings _bindings;
        private TooltipHoverSubscription _tooltipHover;

        public UnitActionsPresenter(IUnitActionsViewProvider coreUi,
            ISelectionService selection,
            IShipAbilityTargeting abilities, UnitActionTargetingModel targeting,
            IPlayerOrderInputHandler inputHandler, IUnitOrderService orders,
            ISkirmishSessionModelObserver session,
            IUiService uiService, IUiCancelRouter cancelRouter,
            ITooltipService tooltips,
            EmpireAtWar.Services.Input.IInputBindings bindings) : base(uiService, cancelRouter)
        {
            _coreUi = coreUi;
            _selection = selection;
            _abilities = abilities;
            _targeting = targeting;
            _inputHandler = inputHandler;
            _orders = orders;
            _session = session;
            _tooltips = new TooltipRequests(tooltips);
            _bindings = bindings;
        }

        public void Initialize()
        {
            _view = _coreUi.UnitActionsView;
            _view.Initialize();
            if (_view is ITooltipHoverView hover)
                _tooltipHover = new TooltipHoverSubscription(
                    hover.TooltipHover, HandleTooltipHover, _tooltips);
            _view.ActionPressed += HandleAction;
            _selection.AddObserver(this);
            _abilities.TargetingChanged += HandleAbilityTargeting;
            _targeting.Changed += RefreshPending;
            RefreshAvailability();
        }

        public void LateDispose()
        {
            if (_tooltipHover != null) _tooltipHover.Dispose();
            _view.ActionPressed -= HandleAction;
            _selection.RemoveObserver(this);
            Unfocus();
            _abilities.TargetingChanged -= HandleAbilityTargeting;
            _targeting.Changed -= RefreshPending;
            _view.Dispose();
            _targeting.Cancel();
        }

        private void HandleTooltipHover(object key, TooltipAnchor anchor, object source)
        {
            UnitActionId action = Enum.Parse<UnitActionId>((string)key);
            _tooltips.Show(source, action, anchor, () => !_session.IsBattleEnded,
                () => BuildTooltip(action));
        }

        private TooltipContent BuildTooltip(UnitActionId action)
        {
            string command = TooltipBindings.Get(_bindings, "Battle", "Command");
            string cancel = TooltipBindings.Get(_bindings, "Ui", "Cancel");
            string description = action switch
            {
                UnitActionId.Attack => $"Attack an enemy unit or hardpoint. Choose a target with {command}. Cancel with {cancel}.",
                UnitActionId.AttackMove => $"Move to a point and engage threats on the way. Choose empty space with {command}. Cancel with {cancel}.",
                UnitActionId.Stop => "Stop movement and clear the current order.",
                UnitActionId.Guard => $"Follow and protect a friendly or allied unit. Engage threats, then return. Choose with {command}; cancel with {cancel}.",
                UnitActionId.WaypointMove => $"Choose successive points with {command}. Click this command again to finish. Cancel with {cancel}.",
                UnitActionId.Hunt => "Seek and engage enemy units automatically.",
                UnitActionId.Retreat => "Withdraw from the battlefield.",
                UnitActionId.Move => $"Choose empty space with {command} to move the selected units.",
                _ => throw new ArgumentOutOfRangeException(nameof(action))
            };
            string shortcut = action == UnitActionId.WaypointMove
                ? TooltipBindings.Get(_bindings, "Battle", "QueueWaypoint") : "";
            return new TooltipContent(action.ToString(), description,
                shortcut: shortcut,
                requirements: _availability[action] ? null : new[]
                {
                    new TooltipRequirement("Select a unit that supports this command", false)
                });
        }

        public void UpdateState(ISelectionSubject subject)
        {
            if (subject.UpdatedScope != SelectionScope.Local) return;
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
            foreach (UnitActionId action in Enum.GetValues(typeof(UnitActionId)))
            {
                bool available = !_session.IsBattleEnded && HasReceiver(action);
                _availability[action] = available;
                _view.SetAvailable(action, available);
            }
            _view.SetVisible(true);
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

        // A pending target selection makes this the selected UI, so Escape cancels it first.
        protected override bool HandleCancel()
        {
            _targeting.Cancel();
            return true;
        }

        private void RefreshPending()
        {
            _view.SetPending(_targeting.Pending);
            if (_targeting.Pending != null) Focus();
            else Unfocus();
        }
    }
}
