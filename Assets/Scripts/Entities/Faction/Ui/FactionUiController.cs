using System;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Factions;
using EmpireAtWar.Services.UiRouting;
using EmpireAtWar.Ui.Base;
using EmpireAtWar.Views.Factions;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Presenters.Factions
{
    public class FactionUiController : IFactionPresenter, IInitializable,
        ILateDisposable, ISkirmishUiRoute
    {
        private readonly IUiService _uiService;
        private readonly IFactionService _factionService;
        private readonly IPlayerFactionModelObserver _model;
        private readonly PlayerFactionData _data;
        private readonly IUnitRequestFactory _unitRequestFactory;
        private readonly ISkirmishRouteNavigation _routeNavigation;

        private IFactionUi _ui;

        public FactionUiController(
            IUiService uiService,
            IFactionService factionService,
            IPlayerFactionModelObserver model,
            PlayerFactionData data,
            IUnitRequestFactory unitRequestFactory,
            ISkirmishRouteNavigation routeNavigation)
        {
            _uiService = uiService;
            _factionService = factionService;
            _model = model;
            _data = data;
            _unitRequestFactory = unitRequestFactory;
            _routeNavigation = routeNavigation;
        }

        public void Initialize()
        {
            _routeNavigation.RegisterRoute(
                SkirmishUiRoutePosition.Content,
                this);
        }

        public void LateDispose()
        {
            _routeNavigation.UnregisterRoute(
                SkirmishUiRoutePosition.Content,
                this);

            if (_ui != null)
            {
                _ui.Dispose();
            }
        }

        public void TryPurchaseUnit(UnitRequest unitRequest)
        {
            _factionService.TryPurchaseUnit(unitRequest);
        }

        public void Activate(bool isActive, Transform parentTransform)
        {
            if (_ui == null)
            {
                BaseUi ui = _uiService.CreateUi(UiType.Faction, parentTransform);
                _ui = ui as IFactionUi
                    ?? throw new InvalidOperationException(
                        "The faction prefab does not implement IFactionUi.");

                _ui.SetParent(parentTransform);
                _ui.SetModel(_model);
                _ui.SetPresenter(this);
                _ui.SetData(_data);
                _ui.SetUnitRequestFactory(_unitRequestFactory);
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
