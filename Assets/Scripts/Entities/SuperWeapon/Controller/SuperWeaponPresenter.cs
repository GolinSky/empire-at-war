using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Services.Tooltip;
using System;
using System.Collections.Generic;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.SuperWeapons.Ui;
using EmpireAtWar.Entities.UnitActions.Model;
using EmpireAtWar.Services.BattleNotifications;
using EmpireAtWar.Services.PlayerCommands;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.SuperWeapons;
using EmpireAtWar.Services.UiRouting;
using EmpireAtWar.Ui.Base;
using VContainer;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.Stations;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EmpireAtWar.Entities.SuperWeapons.Controller
{
    /// <summary>
    /// Manages orbital charge purchases and targeting from the battle HUD.
    /// Binds to the core UI through its route so it works regardless of container initialization order.
    /// </summary>
    public sealed class SuperWeaponPresenter : UiController, IInitializable, ILateDisposable, ISkirmishUiRoute, ITickable,
        IObserver<BattleState>
    {
        private readonly ILocalPlayer _localPlayer;
        private readonly IInterfaceSfxService _interfaceAudio;
        private readonly InterfaceSfxData _interfaceData;
        private readonly IBattleNotifications _notifications;
        private static readonly SuperWeaponType[] WeaponTypes =
            (SuperWeaponType[])Enum.GetValues(typeof(SuperWeaponType));

        private readonly Dictionary<SuperWeaponType, SuperWeaponState> _lastStates = new();
        private readonly Dictionary<SuperWeaponType, SuperWeaponUnitRequest> _purchases = new();
        private bool _isBattleRunning;
        private readonly ISuperWeaponFireService _superWeaponFireService;
        private readonly ISuperWeaponsViewProvider _viewProvider;
        private readonly ISkirmishRouteNavigation _routeNavigation;
        private readonly IShipAbilityTargeting _abilities;
        private ISuperWeaponsView _view;
        private readonly PlayerFactionModel _faction;
        private readonly FactionRoster _roster;
        private readonly IUnitRequestFactory _requestFactory;
        private readonly IEconomyModelObserver _economy;
        private readonly SecondStationLevelModel _secondStation;
        private readonly IProductionAccess _productionAccess;
        private readonly IInputBindings _bindings;
        private readonly IPointerInput _pointer;
        private readonly InputAction _command;
        private readonly INotifier<BattleState> _battleState;

        private readonly SuperWeaponModel _model;
        private readonly IPlayerCommandSink _commands;
        private readonly OrbitalConstructionCancellation _constructionCancellation;
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
            ITooltipService tooltipService, PlayerFactionModel faction, IInputBindings bindings,
            INotifier<BattleState> battleState, SuperWeaponModel model,
            SuperWeaponTargetingModel targeting,
            UnitActionTargetingModel unitTargeting,
            SuperWeaponData data, IInterfaceSfxService interfaceAudio,
            InterfaceSfxData interfaceData, IPlayerCommandSink commands,
            IBattleNotifications notifications, IPointerInput pointer, InputActionsProvider inputActions,
            FactionRoster roster, IUnitRequestFactory requestFactory, IEconomyModelObserver economy,
            SecondStationLevelModel secondStation, IProductionAccess productionAccess,
            OrbitalConstructionCancellation constructionCancellation)
            : base(uiService, cancelRouter)
        {
            _notifications = notifications;
            _commands = commands;
            _constructionCancellation = constructionCancellation;
            _localPlayer = localPlayer;
            _interfaceAudio = interfaceAudio;
            _interfaceData = interfaceData;
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
            _roster = roster;
            _requestFactory = requestFactory;
            _economy = economy;
            _secondStation = secondStation;
            _productionAccess = productionAccess;
            _bindings = bindings;
            _pointer = pointer;
            _command = inputActions.Actions.Battle.Command;
            _battleState = battleState;
        }

        public void Initialize()
        {
            _routeNavigation.RegisterRoute(SkirmishUiRoutePosition.SuperWeapon, this);
            _battleState.AddObserver(this);
            _superWeaponFireService.Fired += HandleFired;
        }

        public void LateDispose()
        {
            _superWeaponFireService.Fired -= HandleFired;
            _battleState.RemoveObserver(this);
            _routeNavigation.UnregisterRoute(SkirmishUiRoutePosition.SuperWeapon, this);
            Unbind();
        }

        public void UpdateState(BattleState state)
        {
            _isBattleEnded = state == BattleState.Ended;
            _isBattleRunning = state == BattleState.Running;
        }

        public void Activate(bool isActive)
        {
            if (isActive) Bind();
            else Unbind();
        }

        private void Bind()
        {
            if (_view != null) return;
            _view = _viewProvider.SuperWeaponsView;
            _view.Initialize();
            _view.SetAvailable(true);
            _tooltipHover = new TooltipHoverSubscription(
                _view.TooltipHover,
                HandleTooltipHover, _tooltips);
            _view.Pressed += HandlePressed;
            _view.PurchaseRequested += HandlePurchaseRequested;
            _view.ToggleRequested += TogglePopup;
            _view.CloseRequested += ClosePopup;
            _pointer.PrimaryPressed += HandlePointerPressed;
            _command.started += HandleCommandPressed;
            _model.OnStateChanged += HandleStateChanged;
            _targeting.Changed += RenderPending;
            _targeting.TargetSubmitted += HandleTargetSubmitted;
            _unitTargeting.Changed += HandleUnitTargetingChanged;
            _abilities.TargetingChanged += HandleAbilityTargetingChanged;

            foreach (var entry in _roster.SuperWeapons)
                _purchases.Add(entry.Key, _requestFactory.ConstructUnitRequest(entry.Value, entry.Key));

            foreach (SuperWeaponType type in WeaponTypes)
            {
                _lastStates[type] = _model.GetState(type);
                _view.SetReady(type, _model.CanFire(type));
            }
            RenderPending();
        }

        private void Unbind()
        {
            if (_view == null) return;
            _tooltipHover.Dispose();
            _view.Pressed -= HandlePressed;
            _view.PurchaseRequested -= HandlePurchaseRequested;
            _view.ToggleRequested -= TogglePopup;
            _view.CloseRequested -= ClosePopup;
            _pointer.PrimaryPressed -= HandlePointerPressed;
            _command.started -= HandleCommandPressed;
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
            _purchases.Clear();
        }

        private void HandleTooltipHover(object key, TooltipAnchor anchor, object source)
        {
            SuperWeaponType type = Enum.Parse<SuperWeaponType>((string)key);
            _tooltips.Show(source, type, anchor, () => _isOpen, () =>
            {
                SuperWeaponProfile profile = _data.GetProfile(type);
                SuperWeaponState state = _model.GetState(type);
                ProductionQueueSnapshot queue = FindQueue(_faction.GetProductionQueueSnapshots(), type);
                string detail = GetRowDetail(type, state, queue, out bool canAct, out _);
                if (canAct && queue != null) detail += " · Click the row for cancellation options";
                else if (canAct && state == SuperWeaponState.Ready) detail += " · Click the row to aim";
                return BuildTooltip(type, profile, detail);
            });
        }

        private TooltipContent BuildTooltip(SuperWeaponType type, SuperWeaponProfile profile, string status)
        {
            (string title, string effect) = type switch
            {
                SuperWeaponType.IonCannon => ("Ion Cannon", "Deals shield damage and disables the target on each hit."),
                SuperWeaponType.HypervelocityGun => ("Hypervelocity Gun", "Deals extra damage to shields and reduced hull damage to structures."),
                SuperWeaponType.PlasmaCannon => ("Plasma Cannon", "Bypasses shields. The blast also hits nearby units on the target's team."),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
            string damageLabel = type == SuperWeaponType.IonCannon ? "Shield damage per shot" : "Base damage per shot";
            var stats = new List<TooltipStat>
            {
                new TooltipStat(damageLabel, profile.Weapon.Damage),
                new TooltipStat("Shots", profile.Weapon.ShotsPerSalvo)
            };
            if (profile.FiringDelay > 0f)
                stats.Add(new TooltipStat("Firing delay (s)", profile.FiringDelay, format: "0.##"));
            if (profile.Weapon.ShotsPerSalvo > 1 && profile.Weapon.ShotInterval > 0f)
                stats.Add(new TooltipStat("Shot interval (s)", profile.Weapon.ShotInterval, format: "0.##"));
            if (profile.AreaDamage > 0f)
            {
                stats.Add(new TooltipStat("Base area damage", profile.AreaDamage));
                stats.Add(new TooltipStat("Area radius", profile.AreaRadius));
            }
            if (profile.StunDuration > 0f)
                stats.Add(new TooltipStat("Disable duration (s)", profile.StunDuration));

            return new TooltipContent(title: title, stats: stats, status: status,
                description: $"{effect} Targets hostile ships and structures at any range. Strikecraft are excluded. " +
                    $"Choose with {TooltipBindings.Get(_bindings, "Battle", "Command")}. " +
                    $"Cancel with {TooltipBindings.Get(_bindings, "Ui", "Cancel")}.");
        }

        private void HandlePressed(SuperWeaponType type)
        {
            if (_isBattleEnded || !_isOpen) return;
            if (_targeting.Pending == type)
            {
                _targeting.Cancel();
                return;
            }

            ProductionQueueSnapshot queue = FindQueue(_faction.GetProductionQueueSnapshots(), type);
            if (queue != null)
            {
                if (_economy != null)
                    _constructionCancellation.Request((SuperWeaponUnitRequest)queue.UnitRequest);
                return;
            }

            if (!_model.CanFire(type)) return;
            _unitTargeting.Cancel();
            _abilities.CancelTargeting();
            // The dropdown only picks the weapon; the target is chosen on the battlefield.
            _isOpen = false;
            _view.SetOpen(false);
            _tooltips.HideAll();
            _targeting.Start(type);
        }

        private void HandlePurchaseRequested(SuperWeaponType type)
        {
            if (_isBattleEnded || !_isOpen || !_model.CanPurchase(type)) return;
            if (_purchases.TryGetValue(type, out SuperWeaponUnitRequest request) &&
                GetPurchaseBlocker(request) == null && _economy.Money >= request.FactionData.Price)
            {
                _commands.Submit(PurchaseUnitCommand.From(_localPlayer.Id, request));
                return;
            }
            _interfaceAudio.PlayRejected();
            _view.BlinkRejected(type);
        }

        private void HandleTargetSubmitted(SuperWeaponType type, IEntity target)
        {
            if (_isBattleEnded) return;
            // Invalid picks such as fighters keep the weapon waiting for a proper target.
            if (!_superWeaponFireService.CanTarget(_localPlayer.Id, target))
            {
                _interfaceAudio.PlayRejected();
                _notifications.Post(BattleNotificationPriority.Feedback, "Invalid target", "invalid-target");
                return;
            }
            _targeting.Accept(target);
            _commands.Submit(new FireSuperWeaponCommand(_localPlayer.Id, type, target.Id));
        }

        /// <summary>Announces accepted local firing and warns the local side of incoming hostile strikes.</summary>
        private void HandleFired(PlayerId issuer, SuperWeaponType type, IEntity target)
        {
            if (!_isBattleRunning || issuer != _localPlayer.Id && !_localPlayer.IsFriendly(target.Owner)) return;
            _interfaceAudio.PlayAnnouncement(_interfaceData.GetOrbitalFiring(type));
        }

        private void HandleStateChanged(SuperWeaponType type, SuperWeaponState state)
        {
            SuperWeaponState previous = _lastStates[type];
            _lastStates[type] = state;
            _view.SetReady(type, _model.CanFire(type));
            if (!_model.CanFire(type) && _targeting.Pending == type) _targeting.Cancel();
            if (_model.IsRestoring || !_isBattleRunning) return;
            if (previous == SuperWeaponState.Unavailable && state == SuperWeaponState.Charging)
                _interfaceAudio.PlayAnnouncement(_interfaceData.GetOrbitalBought(type));
            else if (state == SuperWeaponState.Ready && _model.CanFire(type))
                _interfaceAudio.PlayAnnouncement(_interfaceData.GetOrbitalAvailable(type));
        }

        private void HandleUnitTargetingChanged()
        {
            if (_unitTargeting.Pending != null) ClosePopup();
        }

        private void HandleAbilityTargetingChanged()
        {
            if (_abilities.IsWaitingForTarget) ClosePopup();
        }

        private void HandlePointerPressed(Vector2 position)
        {
            if (_isOpen && !_constructionCancellation.IsPending && !_view.ContainsScreenPoint(position)) ClosePopup();
        }

        private void HandleCommandPressed(InputAction.CallbackContext context) => HandlePointerPressed(_pointer.Position);

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
            RenderRows();
            _view.SetOpen(true);
            RenderPending();
        }

        private void ClosePopup()
        {
            _isOpen = false;
            _view.SetOpen(false);
            _tooltips.HideAll();
            _targeting.Cancel();
            RenderPending();
        }

        public void Tick()
        {
            if (_view == null) return;
            _view.SetBattleAvailable(!_isBattleEnded);
            if (_isBattleEnded)
            {
                if (_isOpen || _targeting.Pending != null) ClosePopup();
                return;
            }
            if (_isOpen) RenderRows();
        }

        private void RenderRows()
        {
            IReadOnlyList<ProductionQueueSnapshot> queues = _faction.GetProductionQueueSnapshots();
            foreach (SuperWeaponType type in WeaponTypes)
            {
                ProductionQueueSnapshot queue = FindQueue(queues, type);
                SuperWeaponState state = queue != null ? SuperWeaponState.Charging : _model.GetState(type);
                string detail = GetRowDetail(type, state, queue, out bool canAct, out float progress);
                _view.SetRow(type, state, canAct, detail, progress);
            }
        }

        private string GetRowDetail(SuperWeaponType type, SuperWeaponState state, ProductionQueueSnapshot queue,
            out bool canAct, out float progress)
        {
            float cooldown = _model.GetCooldownRemaining(type);
            string cooldownText = cooldown > 0f ? $" · Cooldown {FormatTime(cooldown)}" : "";
            progress = 0f;
            if (queue != null)
            {
                canAct = _economy != null;
                progress = Mathf.Clamp01(1f - queue.RemainingBuildTime / queue.UnitRequest.FactionData.BuildTime);
                return $"Building {FormatTime(queue.RemainingBuildTime)}{cooldownText}";
            }

            if (state == SuperWeaponState.Charging)
            {
                canAct = false;
                return $"Building{cooldownText}";
            }

            if (state == SuperWeaponState.Ready)
            {
                canAct = _model.CanFire(type);
                return canAct ? "Ready · 1 charge" : $"Cooldown {FormatTime(cooldown)}";
            }

            if (!_purchases.TryGetValue(type, out SuperWeaponUnitRequest request))
            {
                canAct = false;
                return "Unavailable for this faction";
            }

            string blocker = GetPurchaseBlocker(request);
            FactionData data = request.FactionData;
            canAct = blocker == null && _economy.Money >= data.Price;
            return blocker ?? $"{data.Price:N0} CR · {data.BuildTime:0} s build{cooldownText}";
        }

        private string GetPurchaseBlocker(SuperWeaponUnitRequest request)
        {
            if (_economy == null) return "Purchases unavailable";
            if (!_productionAccess.CanProduce || !_productionAccess.HasHostBase) return "Requires a production station";
            FactionData data = request.FactionData;
            int level = Math.Max(_faction.CurrentLevel, _secondStation.IsBuilt ? _secondStation.CurrentLevel : 1);
            if (level < data.AvailableLevel) return $"Requires station level {data.AvailableLevel}";
            return _faction.CanQueueUnit(request) ? null : "Production queue full";
        }

        private static ProductionQueueSnapshot FindQueue(IReadOnlyList<ProductionQueueSnapshot> queues,
            SuperWeaponType type)
        {
            foreach (ProductionQueueSnapshot queue in queues)
                if (queue.UnitRequest is SuperWeaponUnitRequest request && request.Key == type) return queue;
            return null;
        }

        private static string FormatTime(float seconds)
        {
            int rounded = Mathf.CeilToInt(seconds);
            return $"{rounded / 60:00}:{rounded % 60:00}";
        }
    }
}
