using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Services.Tooltip;
using System;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Services.UiRouting;
using EmpireAtWar.Ui.Base;
using VContainer;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Services.Economy;

namespace EmpireAtWar.Entities.Economy.Ui
{
    public class EconomyUiController : UiController, IInitializable, ILateDisposable,
        ISkirmishUiRoute
    {
        private readonly IEconomyModelObserver _model;
        private readonly ISkirmishRouteNavigation _routeNavigation;
        private EconomyUi _ui;

        private readonly TooltipRequests _tooltips;
        private readonly EconomyService _economy;
        private readonly EconomyData _data;
        private readonly ReinforcementModel _reinforcements;
        private TooltipHoverSubscription _tooltipHover;

        private bool _isTooltipActive;

        private bool _isDisposed;

        public EconomyUiController(
            IUiService uiService,
            IUiCancelRouter cancelRouter,
            IEconomyModelObserver model,
            ISkirmishRouteNavigation routeNavigation,
            ITooltipService tooltipService,
            EconomyService economyService,
            EconomyData data,
            ReinforcementModel reinforcements) : base(uiService, cancelRouter)
        {
            _model = model;
            _routeNavigation = routeNavigation;
            _tooltips = new TooltipRequests(tooltipService);
            _economy = economyService;
            _data = data;
            _reinforcements = reinforcements;
        }

        public void Initialize()
        {
            _routeNavigation.RegisterRoute(
                SkirmishUiRoutePosition.Economy,
                this);
        }

        public void LateDispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _routeNavigation.UnregisterRoute(
                SkirmishUiRoutePosition.Economy,
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
                _ui = CreateUi<EconomyUi>();

                _ui.SetModel(_model);
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

        private void HandleTooltipHover(object key, TooltipAnchor anchor, object source) =>
            _tooltips.Show(source, key, anchor, () => _isTooltipActive, () =>
                new TooltipContent(title: "Economy",
                    description: $"Income is paid every {_data.IncomeDelay:0.#} s. Station levels and your team's mining facilities increase income.",
                    stats: new[]
                    {
                        new TooltipStat(label: "Credits", current: _model.Money, format: "0"),
                        new TooltipStat(label: "Income per payment", current: _economy.TotalIncome, format: "0"),
                        new TooltipStat(label: "Population", current: _reinforcements.CurrentUnitCapacity,
                            max: _reinforcements.MaxUnitCapacity)
                    }));
    }
}
