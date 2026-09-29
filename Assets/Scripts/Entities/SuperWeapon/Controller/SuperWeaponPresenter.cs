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

        public SuperWeaponPresenter(SuperWeaponModel model, SuperWeaponTargetingModel targeting,
            ISuperWeaponFireService fireService, ISuperWeaponsViewProvider viewProvider,
            ISkirmishRouteNavigation routeNavigation,
            UnitActionTargetingModel unitTargeting, IShipAbilityTargeting abilities,
            ILocalPlayer localPlayer, IUiService uiService, IUiCancelRouter cancelRouter)
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
