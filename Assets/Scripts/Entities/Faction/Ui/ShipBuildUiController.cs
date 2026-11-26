using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Services.Tooltip;
using System;
using System.Collections.Generic;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.PlayerCommands;
using EmpireAtWar.Services.SuperWeapons;
using EmpireAtWar.Services.UiRouting;
using EmpireAtWar.Ui.Base;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Entities.Faction.Ui
{
    public class ShipBuildUiController : UiController, IShipBuildPresenter, IInitializable,
        ILateDisposable, ISkirmishUiRoute, IBuildQueueCancelObserver
    {
        private readonly IPlayerCommandSink _commands;
        private readonly OrbitalConstructionCancellation _orbitalCancellation;
        private readonly PlayerSlot _owner;
        private readonly IPlayerFactionModelObserver _model;
        private readonly ISkirmishRouteNavigation _routeNavigation;
        private ShipBuildUi _ui;

        private readonly TooltipRequests _tooltips;
        private readonly UnitRosterTooltips _unitTooltips;
        private TooltipHoverSubscription _tooltipHover;

        private bool _isTooltipActive;

        private bool _isDisposed;

        public event System.Action Canceled;

        public ShipBuildUiController(
            IUiService uiService,
            IUiCancelRouter cancelRouter,
            IPlayerCommandSink commands,
            PlayerSlot owner,
            IPlayerFactionModelObserver model,
            ISkirmishRouteNavigation routeNavigation,
            ITooltipService tooltipService, OrbitalConstructionCancellation orbitalCancellation,
            UnitRosterTooltips unitTooltips) : base(uiService, cancelRouter)
        {
            _unitTooltips = unitTooltips;
            _commands = commands;
            _orbitalCancellation = orbitalCancellation;
            _owner = owner;
            _model = model;
            _routeNavigation = routeNavigation;
            _tooltips = new TooltipRequests(tooltipService);
        }

        public void Initialize()
        {
            _model.OnProductionChanged += RenderPipelines;
            _routeNavigation.RegisterRoute(
                SkirmishUiRoutePosition.BuildPipeline,
                this);
        }

        public void LateDispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _model.OnProductionChanged -= RenderPipelines;
            _routeNavigation.UnregisterRoute(
                SkirmishUiRoutePosition.BuildPipeline,
                this);

            if (_tooltipHover != null) _tooltipHover.Dispose();
            if (_ui != null)
            {
                _ui.Hide();
                _ui.Dispose();
            }
        }

        public void Activate(bool isActive)
        {
            if (_isDisposed) return;
            _isTooltipActive = isActive;
            if (!isActive) _tooltips.HideAll();
            if (_ui == null)
            {
                _ui = CreateUi<ShipBuildUi>();
                _ui.AssignPresenter(this);
                _ui.Initialize();
                _tooltipHover = new TooltipHoverSubscription(
                    _ui.TooltipHover,
                    HandleTooltipHover, _tooltips);
            }

            _ui.RenderPipelines(_model.GetProductionQueueSnapshots());

            if (isActive)
            {
                _ui.Show();
            }
            else
            {
                _ui.Hide();
            }
        }

        private ProductionQueueSnapshot FindQueue(string id)
        {
            foreach (ProductionQueueSnapshot snapshot in _model.GetProductionQueueSnapshots())
                if (snapshot.UnitRequest.Id == id) return snapshot;
            return null;
        }

        private void HandleTooltipHover(object key, TooltipAnchor anchor, object source) =>
            _tooltips.Show(source, key, anchor, () => _isTooltipActive && FindQueue((string)key) != null, () =>
            {
                ProductionQueueSnapshot snapshot = FindQueue((string)key);
                var data = snapshot.UnitRequest.FactionData;
                string status = snapshot.UnitRequest is SuperWeaponUnitRequest
                    ? "Click for cancellation options."
                    : "Click to cancel the current item and refund its cost.";
                return _unitTooltips.Build(snapshot.UnitRequest, new[]
                {
                    new TooltipStat(label: "Remaining time (s)", current: snapshot.RemainingBuildTime, format: "0"),
                    new TooltipStat(label: "Progress (s)", current: data.BuildTime - snapshot.RemainingBuildTime, format: "0", max: data.BuildTime),
                    new TooltipStat(label: "Items in this queue", current: snapshot.Count),
                    new TooltipStat(label: "Cancel refund (credits)", current: data.Price)
                }, status: $"<color=#EF4444><b>{status}</b></color>");
            });

        public void CancelBuilding(string id)
        {
            if (FindQueue(id)?.UnitRequest is SuperWeaponUnitRequest orbital)
            {
                _orbitalCancellation.Request(orbital, () => Canceled?.Invoke());
                return;
            }

            _commands.Submit(new CancelPurchaseCommand(_owner.Id, id));
            Canceled?.Invoke();
        }

        private void RenderPipelines(IReadOnlyList<ProductionQueueSnapshot> snapshots)
        {
            if (_ui != null) _ui.RenderPipelines(snapshots);
        }
    }
}
