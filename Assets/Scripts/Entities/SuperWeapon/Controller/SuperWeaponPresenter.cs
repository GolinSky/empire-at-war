using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Services.Tooltip;
using System;
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
    public sealed class SuperWeaponPresenter : UiController, IInitializable, ILateDisposable, ISkirmishUiRoute
    {
        private readonly SuperWeaponModel _model;
        private readonly ILocalPlayer _localPlayer;
        private readonly SuperWeaponTargetingModel _targeting;
        private readonly ISuperWeaponFireService _fireService;
        private readonly ISuperWeaponsViewProvider _viewProvider;
        private readonly ISkirmishRouteNavigation _routeNavigation;
        private readonly UnitActionTargetingModel _unitTargeting;
        private readonly IShipAbilityTargeting _abilities;
        private ISuperWeaponsView _view;
        private readonly TooltipRequests _tooltips;
        private readonly SuperWeaponData _data;
        private readonly EmpireAtWar.Models.Factions.IPlayerFactionModelObserver _faction;
        private readonly EmpireAtWar.Services.Input.IInputBindings _bindings;
        private TooltipHoverSubscription _tooltipHover;

        public SuperWeaponPresenter(SuperWeaponModel model, SuperWeaponTargetingModel targeting,
            ISuperWeaponFireService fireService, ISuperWeaponsViewProvider viewProvider,
            ISkirmishRouteNavigation routeNavigation,
            UnitActionTargetingModel unitTargeting, IShipAbilityTargeting abilities,
            ILocalPlayer localPlayer, IUiService uiService, IUiCancelRouter cancelRouter,
            ITooltipService tooltips, SuperWeaponData data,
            EmpireAtWar.Models.Factions.IPlayerFactionModelObserver faction,
            EmpireAtWar.Services.Input.IInputBindings bindings)
            : base(uiService, cancelRouter)
        {
            _localPlayer = localPlayer;
            _model = model;
            _targeting = targeting;
            _fireService = fireService;
            _viewProvider = viewProvider;
            _routeNavigation = routeNavigation;
            _unitTargeting = unitTargeting;
            _abilities = abilities;
            _tooltips = new TooltipRequests(tooltips);
            _data = data;
            _faction = faction;
            _bindings = bindings;
        }

        public void Initialize()
        {
            _routeNavigation.RegisterRoute(SkirmishUiRoutePosition.SuperWeapon, this);
        }

        public void LateDispose()
        {
            _routeNavigation.UnregisterRoute(SkirmishUiRoutePosition.SuperWeapon, this);
            Unbind();
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
                return new TooltipContent(type.ToString(),
                    $"Fire at a hostile ship or station; fighters are not valid targets. Choose with {TooltipBindings.Get(_bindings, "Battle", "Command")}; cancel with {TooltipBindings.Get(_bindings, "Ui", "Cancel")}.",
                    stats: new[]
                    {
                        new TooltipStat("Damage per shot", profile.Weapon.Damage),
                        new TooltipStat("Shots", profile.Weapon.ShotsPerSalvo),
                        new TooltipStat("Range", profile.Weapon.Range),
                        new TooltipStat("Firing delay (s)", profile.FiringDelay),
                        new TooltipStat("Area damage", profile.AreaDamage),
                        new TooltipStat("Area radius", profile.AreaRadius),
                        new TooltipStat("Stun duration (s)", profile.StunDuration)
                    }, status: state == SuperWeaponState.Charging ? $"Construction: {remaining:0.#} s remaining"
                        : state == SuperWeaponState.Ready ? "Ready: click to select a target"
                        : "Unavailable: build a new charge at the station");
            });
        }

        private void HandlePressed(SuperWeaponType type)
        {
            if (_targeting.Pending == type)
            {
                _targeting.Cancel();
                return;
            }

            if (_model.GetState(type) != SuperWeaponState.Ready) return;
            _unitTargeting.Cancel();
            _abilities.CancelTargeting();
            _targeting.Start(type);
        }

        private void HandleTargetSubmitted(SuperWeaponType type, IEntity target)
        {
            // Invalid picks such as fighters keep the weapon waiting for a proper target.
            if (!_fireService.CanTarget(_localPlayer.Id, target)) return;
            _targeting.Cancel();
            _model.Consume(type);
            _fireService.Fire(type, target);
        }

        private void HandleStateChanged(SuperWeaponType type, SuperWeaponState state)
        {
            _view.SetState(type, state);
            if (state != SuperWeaponState.Ready && _targeting.Pending == type) _targeting.Cancel();
        }

        private void HandleUnitTargetingChanged()
        {
            if (_unitTargeting.Pending != null) _targeting.Cancel();
        }

        private void HandleAbilityTargetingChanged()
        {
            if (_abilities.IsWaitingForTarget) _targeting.Cancel();
        }

        // A pending target selection makes this the selected UI, so Escape cancels it first.
        protected override bool HandleCancel()
        {
            _targeting.Cancel();
            return true;
        }

        private void RenderPending()
        {
            _view.SetPending(_targeting.Pending);
            if (_targeting.Pending != null) Focus();
            else Unfocus();
        }
    }
}
