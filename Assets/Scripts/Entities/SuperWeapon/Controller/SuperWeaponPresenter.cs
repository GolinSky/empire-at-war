using System;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.SuperWeapons.Ui;
using EmpireAtWar.Entities.UnitActions.Model;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.InputService;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.SuperWeapons;
using EmpireAtWar.Services.UiRouting;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.SuperWeapons.Controller
{
    /// <summary>
    /// Player superweapon buttons: a ready weapon starts targeting, the next valid enemy click fires it.
    /// Binds to the core UI through its route so it works regardless of container initialization order.
    /// </summary>
    public sealed class SuperWeaponPresenter : IInitializable, ILateDisposable, ISkirmishUiRoute
    {
        private readonly SuperWeaponModel _model;
        private readonly SuperWeaponTargetingModel _targeting;
        private readonly ISuperWeaponFireService _fireService;
        private readonly ISuperWeaponsViewProvider _viewProvider;
        private readonly ISkirmishRouteNavigation _routeNavigation;
        private readonly IInputService _input;
        private readonly UnitActionTargetingModel _unitTargeting;
        private readonly IShipAbilityTargeting _abilities;
        private ISuperWeaponsView _view;

        public SuperWeaponPresenter(SuperWeaponModel model, SuperWeaponTargetingModel targeting,
            ISuperWeaponFireService fireService, ISuperWeaponsViewProvider viewProvider,
            ISkirmishRouteNavigation routeNavigation, IInputService input,
            UnitActionTargetingModel unitTargeting, IShipAbilityTargeting abilities)
        {
            _model = model;
            _targeting = targeting;
            _fireService = fireService;
            _viewProvider = viewProvider;
            _routeNavigation = routeNavigation;
            _input = input;
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
            _input.OnEscapePressed += _targeting.Cancel;

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
            _input.OnEscapePressed -= _targeting.Cancel;
            _targeting.Cancel();
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
            if (!_fireService.CanTarget(PlayerType.Player, target)) return;
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

        private void RenderPending() => _view.SetPending(_targeting.Pending);
    }
}
