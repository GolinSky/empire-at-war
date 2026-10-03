using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Services.Tooltip;
using System;
using System.Collections.Generic;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Factions;
using EmpireAtWar.Services.UiRouting;
using EmpireAtWar.Ui.Base;
using EmpireAtWar.Views.Factions;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Presenters.Factions
{
    public class ShipBuildUiController : UiController, IShipBuildPresenter, IInitializable,
        ILateDisposable, ISkirmishUiRoute
    {
        private readonly IFactionService _factionService;
        private readonly IPlayerFactionModelObserver _model;
        private readonly ISkirmishRouteNavigation _routeNavigation;
        private IShipBuildUi _ui;

        private readonly TooltipRequests _tooltips;
        private TooltipHoverSubscription _tooltipHover;

        private bool _isTooltipActive;

        public ShipBuildUiController(
            IUiService uiService,
            IUiCancelRouter cancelRouter,
            IFactionService factionService,
            IPlayerFactionModelObserver model,
            ISkirmishRouteNavigation routeNavigation,
            ITooltipService tooltips) : base(uiService, cancelRouter)
        {
            _factionService = factionService;
            _model = model;
            _routeNavigation = routeNavigation;
            _tooltips = new TooltipRequests(tooltips);
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
            _model.OnProductionChanged -= RenderPipelines;
            _routeNavigation.UnregisterRoute(
                SkirmishUiRoutePosition.BuildPipeline,
                this);

            if (_ui != null)
            {
                _tooltipHover.Dispose();
                _ui.Dispose();
            }
        }

        public void Activate(bool isActive, Transform parentTransform)
        {
            _isTooltipActive = isActive;
            if (!isActive) _tooltips.HideAll();
            if (_ui == null)
            {
                BaseUi ui = UiService.CreateUi(UiType.ShipBuild, parentTransform);
                _ui = ui as IShipBuildUi
                    ?? throw new InvalidOperationException(
                        "The ship build prefab does not implement IShipBuildUi.");
                _ui.SetPresenter(this);
                _ui.Initialize();
                _tooltipHover = new TooltipHoverSubscription(
                    ((ITooltipHoverView)_ui).TooltipHover,
                    HandleTooltipHover, _tooltips);
            }
            else
            {
                _ui.SetParent(parentTransform);
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
            _ui?.RenderPipelines(snapshots);
        }
    }
}
