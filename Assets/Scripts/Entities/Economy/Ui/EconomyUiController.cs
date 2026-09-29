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

        public EconomyUiController(
            IUiService uiService,
            IUiCancelRouter cancelRouter,
            IEconomyModelObserver model,
            ISkirmishRouteNavigation routeNavigation) : base(uiService, cancelRouter)
        {
            _model = model;
            _routeNavigation = routeNavigation;
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
                _ui.Dispose();
            }
        }

        public void Activate(bool isActive, Transform parentTransform)
        {
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
    }
}
