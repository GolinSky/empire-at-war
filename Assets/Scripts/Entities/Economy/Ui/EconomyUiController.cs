using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Services.Tooltip;
using System;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Services.UiRouting;
using EmpireAtWar.Ui.Base;
using EmpireAtWar.Views.Economy;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Presenters.Economy
{
    public class EconomyUiController : UiController, IInitializable, ILateDisposable,
        ISkirmishUiRoute
    {
        private readonly IEconomyModelObserver _model;
        private readonly ISkirmishRouteNavigation _routeNavigation;
        private IEconomyUi _ui;

        private readonly TooltipRequests _tooltips;
        private readonly EmpireAtWar.Services.Economy.EconomyService _economy;
        private readonly EmpireAtWar.Models.Economy.EconomyData _data;
        private readonly EmpireAtWar.Models.Reinforcement.ReinforcementModel _reinforcements;
        private TooltipHoverSubscription _tooltipHover;

        private bool _isTooltipActive;

        public EconomyUiController(
            IUiService uiService,
            IUiCancelRouter cancelRouter,
            IEconomyModelObserver model,
            ISkirmishRouteNavigation routeNavigation,
            ITooltipService tooltips,
            EmpireAtWar.Services.Economy.EconomyService economy,
            EmpireAtWar.Models.Economy.EconomyData data,
            EmpireAtWar.Models.Reinforcement.ReinforcementModel reinforcements) : base(uiService, cancelRouter)
        {
            _model = model;
            _routeNavigation = routeNavigation;
            _tooltips = new TooltipRequests(tooltips);
            _economy = economy;
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
            _routeNavigation.UnregisterRoute(
                SkirmishUiRoutePosition.Economy,
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
                BaseUi ui = UiService.CreateUi(
                    UiType.Economy,
                    parentTransform);
                _ui = ui as IEconomyUi
                    ?? throw new InvalidOperationException(
                        "The economy prefab does not implement IEconomyUi.");

                _ui.SetModel(_model);
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

        private void HandleTooltipHover(object key, TooltipAnchor anchor, object source) =>
            _tooltips.Show(source, key, anchor, () => _isTooltipActive, () =>
                new TooltipContent(title: "Economy",
                    description: $"Income is paid every {_data.IncomeDelay:0.#} s. Station levels and mining facilities increase income.",
                    stats: new[]
                    {
                        new TooltipStat(label: "Credits", current: _model.Money),
                        new TooltipStat(label: "Income per payment", current: _economy.TotalIncome),
                        new TooltipStat(label: "Population", current: _reinforcements.CurrentUnitCapacity,
                            max: _reinforcements.MaxUnitCapacity)
                    }));
    }
}
