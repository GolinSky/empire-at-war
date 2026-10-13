using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Services.Tooltip;
using System;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Services.Reinforcement;
using EmpireAtWar.Services.UiRouting;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Entities.Reinforcement
{
    public class ReinforcementUiController : UiController, IReinforcementPresenter,
        IInitializable, ILateDisposable, ISkirmishUiRoute
    {
        private readonly IReinforcementService _reinforcementService;
        private readonly ISkirmishRouteNavigation _routeNavigation;
        private ReinforcementUi _ui;

        private readonly ReinforcementModel _model;
        private readonly ReinforcementData _data;
        private readonly TooltipRequests _tooltips;
        private TooltipHoverSubscription _tooltipHover;

        private bool _isTooltipActive;

        private bool _isDisposed;

        public ReinforcementUiController(
            IUiService uiService,
            IUiCancelRouter cancelRouter,
            IReinforcementService reinforcementService,
            ISkirmishRouteNavigation routeNavigation,
            ITooltipService tooltipService,
            ReinforcementModel model,
            ReinforcementData data) : base(uiService, cancelRouter)
        {
            _reinforcementService = reinforcementService;
            _model = model;
            _data = data;
            _routeNavigation = routeNavigation;
            _tooltips = new TooltipRequests(tooltipService);
        }

        public void Initialize()
        {
            _routeNavigation.SetRouteActive(
                SkirmishUiRoutePosition.Reinforcement,
                false);
            _routeNavigation.RegisterRoute(
                SkirmishUiRoutePosition.Reinforcement,
                this);
        }

        public void LateDispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _routeNavigation.UnregisterRoute(
                SkirmishUiRoutePosition.Reinforcement,
                this);

            if (_tooltipHover != null) _tooltipHover.Dispose();
            if (_ui != null)
            {
                _ui.Hide();
                _ui.Dispose();
            }
        }

        public void TrySpawnReinforcement(UnitRequest request)
        {
            _reinforcementService.TrySpawnReinforcement(request);
        }

        public void Show()
        {
            _routeNavigation.SetRouteActive(
                SkirmishUiRoutePosition.Reinforcement,
                true);
        }

        public void Hide()
        {
            _routeNavigation.SetRouteActive(
                SkirmishUiRoutePosition.Reinforcement,
                false);
        }

        public void Activate(bool isActive)
        {
            if (_isDisposed) return;
            _isTooltipActive = isActive;
            if (!isActive) _tooltips.HideAll();
            if (_ui == null)
            {
                _ui = CreateUi<ReinforcementUi>();

                _ui.SetModel(_model);
                _ui.AssignPresenter(this);
                _ui.SetData(_data);
                _ui.Initialize();
                _tooltipHover = new TooltipHoverSubscription(
                    _ui.TooltipHover,
                    HandleTooltipHover, _tooltips);
            }

            if (isActive)
            {
                _ui.Show();
            }
            else
            {
                _ui.Hide();
            }
        }

        private void HandleTooltipHover(object key, TooltipAnchor anchor, object source)
        {
            if (key is UnitRequest request)
                _tooltips.Show(source, request.Id, anchor, () => _isTooltipActive && _model.GetReserveCount(request) > 0,
                    () => UnitTooltipContent.Build(request.FactionData, new[]
                    {
                        new TooltipStat(label: "Available reserves", current: _model.GetReserveCount(request)),
                        new TooltipStat(label: "Population", current: request.FactionData.UnitCapacity),
                        new TooltipStat(label: "Used population", current: _model.CurrentUnitCapacity, max: _model.MaxUnitCapacity)
                    }, status: request.FactionData.UnitCapacity > _model.CapacityLeft
                        ? "Population limit reached" : request is ShipUnitRequest || request is EmpireAtWar.Controllers.Factions.SquadronUnitRequest
                            ? "Drag into an allied reinforcement zone. Ships need clear space. Release to deploy immediately."
                            : "Drag to visible terrain outside reinforcement zones and capture sites. Release to deploy."));
            else
                _tooltips.Show(source, key, anchor, () => _isTooltipActive,
                    () => new TooltipContent(title: "Reinforcements", description: "Close the reinforcement panel."));
        }
    }
}
