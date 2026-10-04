using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Services.Tooltip;
using System;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.SuperWeapons.Ui;
using EmpireAtWar.Entities.UnitActions.Model;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.SuperWeapons;
using EmpireAtWar.Services.UiRouting;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.SuperWeapons.Controller
{
    /// <summary>
    /// Player superweapon buttons: a ready weapon starts targeting, the next valid enemy click fires it.
    /// Binds to the core UI through its route so it works regardless of container initialization order.
    /// </summary>
    public sealed class SuperWeaponPresenter : UiController, IInitializable, ILateDisposable, ISkirmishUiRoute, ITickable,
        IObserver<BattleState>
    {
        private readonly ILocalPlayer _localPlayer;
        private readonly ISuperWeaponFireService _superWeaponFireService;
        private readonly ISuperWeaponsViewProvider _viewProvider;
        private readonly ISkirmishRouteNavigation _routeNavigation;
        private readonly IShipAbilityTargeting _abilities;
        private ISuperWeaponsView _view;
        private readonly EmpireAtWar.Models.Factions.IPlayerFactionModelObserver _faction;
        private readonly EmpireAtWar.Services.Input.IInputBindings _bindings;
        private readonly INotifier<BattleState> _battleState;

        private readonly SuperWeaponModel _model;
        private readonly SuperWeaponTargetingModel _targeting;
        private readonly UnitActionTargetingModel _unitTargeting;
        private readonly TooltipRequests _tooltips;
        private readonly SuperWeaponData _data;
        private TooltipHoverSubscription _tooltipHover;

        private bool _isOpen;
        private bool _isBattleEnded;

        public SuperWeaponPresenter(ISuperWeaponFireService superWeaponFireService, ISuperWeaponsViewProvider viewProvider,
            ISkirmishRouteNavigation routeNavigation, IShipAbilityTargeting abilities,
            ILocalPlayer localPlayer,
            IUiService uiService, IUiCancelRouter cancelRouter,
            ITooltipService tooltipService, EmpireAtWar.Models.Factions.IPlayerFactionModelObserver faction, EmpireAtWar.Services.Input.IInputBindings bindings,
            INotifier<BattleState> battleState, SuperWeaponModel model,
            SuperWeaponTargetingModel targeting,
            UnitActionTargetingModel unitTargeting,
            SuperWeaponData data)
            : base(uiService, cancelRouter)
        {
            _localPlayer = localPlayer;
            _model = model;
            _targeting = targeting;
            _superWeaponFireService = superWeaponFireService;
            _viewProvider = viewProvider;
            _routeNavigation = routeNavigation;
            _unitTargeting = unitTargeting;
            _abilities = abilities;
            _tooltips = new TooltipRequests(tooltipService);
            _data = data;
            _faction = faction;
            _bindings = bindings;
            _battleState = battleState;
        }

        public void Initialize()
        {
            _routeNavigation.RegisterRoute(SkirmishUiRoutePosition.SuperWeapon, this);
            _battleState.AddObserver(this);
        }

        public void LateDispose()
        {
            _battleState.RemoveObserver(this);
            _routeNavigation.UnregisterRoute(SkirmishUiRoutePosition.SuperWeapon, this);
            Unbind();
        }

        public void UpdateState(BattleState state)
        {
            _isBattleEnded = state == BattleState.Ended;
        }

        public void Activate(bool isActive, Transform parentTransform)
        {
            if (isActive) Bind();
            else Unbind();
        }

        private void Bind()
        {
            if (_view != null) return;
            _view = _viewProvider.SuperWeaponsView;
            _view.Initialize();
            _tooltipHover = new TooltipHoverSubscription(
                ((ITooltipHoverView)_view).TooltipHover,
                HandleTooltipHover, _tooltips);
            _view.Pressed += HandlePressed;
            _view.ToggleRequested += TogglePopup;
            _view.CloseRequested += ClosePopup;
            _model.OnStateChanged += HandleStateChanged;
            _targeting.Changed += RenderPending;
            _targeting.TargetSubmitted += HandleTargetSubmitted;
            _unitTargeting.Changed += HandleUnitTargetingChanged;
            _abilities.TargetingChanged += HandleAbilityTargetingChanged;

            foreach (SuperWeaponType type in Enum.GetValues(typeof(SuperWeaponType)))
                _view.SetState(type, _model.GetState(type));
            RenderPending();
        }

        private void Unbind()
        {
            if (_view == null) return;
            _tooltipHover.Dispose();
            _view.Pressed -= HandlePressed;
            _view.ToggleRequested -= TogglePopup;
            _view.CloseRequested -= ClosePopup;
            _isOpen = false;
            _model.OnStateChanged -= HandleStateChanged;
            _targeting.Changed -= RenderPending;
            _targeting.TargetSubmitted -= HandleTargetSubmitted;
            _unitTargeting.Changed -= HandleUnitTargetingChanged;
            _abilities.TargetingChanged -= HandleAbilityTargetingChanged;
            _targeting.Cancel();
            Unfocus();
            _view.Dispose();
            _view = null;
        }

        private void HandleTooltipHover(object key, TooltipAnchor anchor, object source)
        {
            SuperWeaponType type = Enum.Parse<SuperWeaponType>((string)key);
            _tooltips.Show(source, type, anchor, () => _view != null, () =>
            {
                SuperWeaponProfile profile = _data.GetProfile(type);
                SuperWeaponState state = _model.GetState(type);
                float remaining = 0f;
                foreach (var queue in _faction.GetProductionQueueSnapshots())
                    if (queue.UnitRequest is EmpireAtWar.Controllers.Factions.SuperWeaponUnitRequest request && request.Key == type)
                        remaining = queue.RemainingBuildTime;
                return new TooltipContent(title: type.ToString(),
                    description: $"Fire at a hostile ship or station; fighters are not valid targets. Choose with {TooltipBindings.Get(_bindings, "Battle", "Command")}; cancel with {TooltipBindings.Get(_bindings, "Ui", "Cancel")}.",
                    stats: new[]
                    {
                        new TooltipStat(label: "Damage per shot", current: profile.Weapon.Damage),
                        new TooltipStat(label: "Shots", current: profile.Weapon.ShotsPerSalvo),
                        new TooltipStat(label: "Range", current: profile.Weapon.Range),
                        new TooltipStat(label: "Firing delay (s)", current: profile.FiringDelay),
                        new TooltipStat(label: "Area damage", current: profile.AreaDamage),
                        new TooltipStat(label: "Area radius", current: profile.AreaRadius),
                        new TooltipStat(label: "Stun duration (s)", current: profile.StunDuration)
                    }, status: state == SuperWeaponState.Charging ? $"Construction: {remaining:0.#} s remaining"
                        : state == SuperWeaponState.Ready ? "Ready: click to select a target"
                        : state == SuperWeaponState.Cooldown ? $"Cooldown: {_model.GetCooldownRemaining(type):0.#} s remaining"
                        : "Unavailable: build a new charge at the station");
            });
        }

        private void HandlePressed(SuperWeaponType type)
        {
            if (_isBattleEnded) return;
            if (_targeting.Pending == type)
            {
                _targeting.Cancel();
                return;
            }

            if (_model.GetState(type) != SuperWeaponState.Ready) return;
            _unitTargeting.Cancel();
            _abilities.CancelTargeting();
            // The dropdown only picks the weapon; the target is chosen on the battlefield.
            _isOpen = false;
            _view.SetOpen(false);
            _targeting.Start(type);
        }

        private void HandleTargetSubmitted(SuperWeaponType type, IEntity target)
        {
            if (_isBattleEnded) return;
            // Invalid picks such as fighters keep the weapon waiting for a proper target.
            if (!_superWeaponFireService.CanTarget(_localPlayer.Id, target)) return;
            _targeting.Cancel();
            _model.Consume(type);
            _superWeaponFireService.Fire(type, target);
        }

        private void HandleStateChanged(SuperWeaponType type, SuperWeaponState state)
        {
            _view.SetState(type, state);
            if (state != SuperWeaponState.Ready && _targeting.Pending == type) _targeting.Cancel();
        }

        private void HandleUnitTargetingChanged()
        {
            if (_unitTargeting.Pending != null) ClosePopup();
        }

        private void HandleAbilityTargetingChanged()
        {
            if (_abilities.IsWaitingForTarget) ClosePopup();
        }

        // A pending target selection makes this the selected UI, so Escape cancels it first.
        protected override bool HandleCancel()
        {
            if (_targeting.Pending != null) _targeting.Cancel();
            else ClosePopup();
            return true;
        }

        private void RenderPending()
        {
            _view.SetPending(_targeting.Pending);
            if (_targeting.Pending != null || _isOpen) Focus();
            else Unfocus();
        }

        private void TogglePopup()
        {
            if (_isBattleEnded) return;
            if (_isOpen)
            {
                ClosePopup();
                return;
            }
            _isOpen = true;
            _view.SetOpen(true);
            RenderPending();
        }

        private void ClosePopup()
        {
            _isOpen = false;
            _view.SetOpen(false);
            _targeting.Cancel();
            RenderPending();
        }

        public void Tick()
        {
            _model.Tick(Time.deltaTime);
            if (_view == null) return;
            _view.SetBattleAvailable(!_isBattleEnded);
            if (_isBattleEnded)
            {
                if (_isOpen || _targeting.Pending != null) ClosePopup();
                return;
            }
            if (!_isOpen) return;
            foreach (var queue in _faction.GetProductionQueueSnapshots())
                if (queue.UnitRequest is EmpireAtWar.Controllers.Factions.SuperWeaponUnitRequest request)
                    _view.SetRemaining(request.Key, queue.RemainingBuildTime);
            foreach (SuperWeaponType type in Enum.GetValues(typeof(SuperWeaponType)))
                if (_model.GetState(type) == SuperWeaponState.Cooldown)
                    _view.SetRemaining(type, _model.GetCooldownRemaining(type));
        }
    }
}
