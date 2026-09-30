using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Services.Tooltip;
using System;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Services.Reinforcement;
using EmpireAtWar.Services.UiRouting;
using EmpireAtWar.Ui.Base;
using EmpireAtWar.Views.Reinforcement;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Presenters.Reinforcement
{
    public class ReinforcementUiController : UiController, IReinforcementPresenter,
        IInitializable, ILateDisposable, ISkirmishUiRoute
    {
        private readonly IReinforcementService _reinforcementService;
        private readonly ReinforcementModel _model;
        private readonly ReinforcementData _data;
        private readonly ISkirmishRouteNavigation _routeNavigation;

        private IReinforcementUi _ui;
        private readonly TooltipRequests _tooltips;
        private TooltipHoverSubscription _tooltipHover;
        private bool _isTooltipActive;

        public ReinforcementUiController(
            IUiService uiService,
            IUiCancelRouter cancelRouter,
            IReinforcementService reinforcementService,
            ReinforcementModel model,
            ReinforcementData data,
            ISkirmishRouteNavigation routeNavigation,
            ITooltipService tooltips) : base(uiService, cancelRouter)
        {
            _reinforcementService = reinforcementService;
            _model = model;
            _data = data;
            _routeNavigation = routeNavigation;
            _tooltips = new TooltipRequests(tooltips);
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
            _routeNavigation.UnregisterRoute(
                SkirmishUiRoutePosition.Reinforcement,
                this);

            if (_ui != null)
            {
                _tooltipHover.Dispose();
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

        public void Activate(bool isActive, Transform parentTransform)
        {
            _isTooltipActive = isActive;
            if (!isActive) _tooltips.HideAll();
            if (_ui == null)
            {
                BaseUi ui = UiService.CreateUi(
                    UiType.Reinforcement,
                    parentTransform);
                _ui = ui as IReinforcementUi
                    ?? throw new InvalidOperationException(
                        "The reinforcement prefab does not implement IReinforcementUi.");

                _ui.SetModel(_model);
                _ui.SetPresenter(this);
                _ui.SetData(_data);
                _ui.Initialize();
                _tooltipHover = new TooltipHoverSubscription(
                    ((ITooltipHoverView)_ui).TooltipHover,
                    HandleTooltipHover, _tooltips);
            }
            else
            {
                _ui.SetParent(parentTransform);
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
                        new TooltipStat("Available reserves", _model.GetReserveCount(request)),
                        new TooltipStat("Population", request.FactionData.UnitCapacity),
                        new TooltipStat("Used population", _model.CurrentUnitCapacity, _model.MaxUnitCapacity)
                    }, status: request.FactionData.UnitCapacity > _model.CapacityLeft
                        ? "Population limit reached" : request is ShipUnitRequest || request is EmpireAtWar.Controllers.Factions.SquadronUnitRequest
                            ? "Drag into an allied reinforcement zone. Ships need clear space. Release to deploy immediately."
                            : "Drag to visible terrain outside reinforcement zones and capture sites. Release to deploy."));
            else
                _tooltips.Show(source, key, anchor, () => _isTooltipActive,
                    () => new TooltipContent("Reinforcements", "Close the reinforcement panel."));
        }
    }
}
