using System.Collections.Generic;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Services.Tooltip;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Services.UiRouting;
using EmpireAtWar.Ui.Base;
using VContainer;
using EmpireAtWar.Lifecycle;
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
            EconomyData data) : base(uiService, cancelRouter)
        {
            _model = model;
            _routeNavigation = routeNavigation;
            _tooltips = new TooltipRequests(tooltipService);
            _economy = economyService;
            _data = data;
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
            _tooltips.Show(source, key, anchor, () => _isTooltipActive, BuildTooltip);

        private TooltipContent BuildTooltip()
        {
            const string CREDIT_FORMAT = "0.##' CR'";
            const string CHANGE_FORMAT = "+0.##' CR';-0.##' CR';0' CR'";
            float total = _economy.TotalIncome;
            var breakdown = _economy.IncomeBreakdown;
            var stats = new List<TooltipStat>
            {
                new("Base and stations", total - breakdown.Facilities, CREDIT_FORMAT),
                new("Team facilities", breakdown.Facilities, CREDIT_FORMAT)
            };
            if (breakdown.StationResearchBonus != 0f)
                stats.Add(new TooltipStat("Includes station research", breakdown.StationResearchBonus, CHANGE_FORMAT));
            if (breakdown.FacilityResearchBonus != 0f)
                stats.Add(new TooltipStat("Includes facility research", breakdown.FacilityResearchBonus, CHANGE_FORMAT));
            stats.Add(new TooltipStat("Total per tick", total, CHANGE_FORMAT));
            return new TooltipContent("Credit income", stats: stats,
                description: $"Payments arrive every {_data.IncomeDelay:0.##} s of game time.");
        }
    }
}
