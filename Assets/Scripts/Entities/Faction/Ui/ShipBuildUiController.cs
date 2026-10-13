using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Services.Tooltip;
using System;
using System.Collections.Generic;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Factions;
using EmpireAtWar.Services.UiRouting;
using EmpireAtWar.Ui.Base;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Entities.Faction.Ui
{
    public class ShipBuildUiController : UiController, IShipBuildPresenter, IInitializable,
        ILateDisposable, ISkirmishUiRoute
    {
        private readonly IFactionService _factionService;
        private readonly IPlayerFactionModelObserver _model;
        private readonly ISkirmishRouteNavigation _routeNavigation;
        private ShipBuildUi _ui;

        private readonly TooltipRequests _tooltips;
        private TooltipHoverSubscription _tooltipHover;

        private bool _isTooltipActive;

        private bool _isDisposed;

        public ShipBuildUiController(
            IUiService uiService,
            IUiCancelRouter cancelRouter,
            IFactionService factionService,
            IPlayerFactionModelObserver model,
            ISkirmishRouteNavigation routeNavigation,
            ITooltipService tooltipService) : base(uiService, cancelRouter)
        {
            _factionService = factionService;
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

        private ProductionQueueSnapshot FindTooltipQueue(object key)
        {
            foreach (ProductionQueueSnapshot snapshot in _model.GetProductionQueueSnapshots())
                if (snapshot.UnitRequest.Id == (string)key) return snapshot;
            return null;
        }

        private void HandleTooltipHover(object key, TooltipAnchor anchor, object source) =>
            _tooltips.Show(source, key, anchor, () => _isTooltipActive && FindTooltipQueue(key) != null, () =>
            {
                ProductionQueueSnapshot snapshot = FindTooltipQueue(key);
                var data = snapshot.UnitRequest.FactionData;
                return UnitTooltipContent.Build(data, new[]
                {
                    new TooltipStat(label: "Remaining time (s)", current: snapshot.RemainingBuildTime),
                    new TooltipStat(label: "Progress (s)", current: data.BuildTime - snapshot.RemainingBuildTime, max: data.BuildTime),
                    new TooltipStat(label: "Items in this queue", current: snapshot.Count),
                    new TooltipStat(label: "Cancel refund (credits)", current: data.Price)
                }, status: "Click to cancel the current item and refund its cost.");
            });

        public void CancelBuilding(string id)
        {
            _factionService.CancelBuilding(id);
        }

        private void RenderPipelines(IReadOnlyList<ProductionQueueSnapshot> snapshots)
        {
            if (_ui != null) _ui.RenderPipelines(snapshots);
        }
    }
}
