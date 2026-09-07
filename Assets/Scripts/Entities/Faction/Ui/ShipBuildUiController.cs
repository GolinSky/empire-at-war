using System;
using System.Collections.Generic;
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
    public class ShipBuildUiController : IShipBuildPresenter, IInitializable,
        ILateDisposable, ISkirmishUiRoute
    {
        private readonly IUiService _uiService;
        private readonly IFactionService _factionService;
        private readonly IPlayerFactionModelObserver _model;
        private readonly ISkirmishRouteNavigation _routeNavigation;
        private readonly Dictionary<string, UnitRequest> _unitRequests =
            new Dictionary<string, UnitRequest>();

        private IShipBuildUi _ui;

        public ShipBuildUiController(
            IUiService uiService,
            IFactionService factionService,
            IPlayerFactionModelObserver model,
            ISkirmishRouteNavigation routeNavigation)
        {
            _uiService = uiService ??
                throw new ArgumentNullException(nameof(uiService));
            _factionService = factionService ??
                throw new ArgumentNullException(nameof(factionService));
            _model = model ??
                throw new ArgumentNullException(nameof(model));
            _routeNavigation = routeNavigation ??
                throw new ArgumentNullException(nameof(routeNavigation));
        }

        public void Initialize()
        {
            _model.OnUnitBuild += AddPipeline;
            _routeNavigation.RegisterRoute(
                SkirmishUiRoutePosition.BuildPipeline,
                this);
        }

        public void LateDispose()
        {
            _model.OnUnitBuild -= AddPipeline;
            _routeNavigation.UnregisterRoute(
                SkirmishUiRoutePosition.BuildPipeline,
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
                BaseUi ui = _uiService.CreateUi(UiType.ShipBuild, parentTransform);
                _ui = ui as IShipBuildUi
                    ?? throw new InvalidOperationException(
                        "The ship build prefab does not implement IShipBuildUi.");
                _ui.SetPresenter(this);
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

        public void CompleteBuilding(bool isSuccess, string id)
        {
            if (!_unitRequests.TryGetValue(id, out UnitRequest unitRequest))
            {
                throw new InvalidOperationException(
                    $"No queued unit request exists for id '{id}'.");
            }

            if (isSuccess)
            {
                _factionService.BuildUnit(unitRequest);
            }
            else
            {
                _factionService.RevertBuilding(unitRequest);
            }
        }

        private void AddPipeline(UnitRequest unitRequest)
        {
            if (_ui == null)
            {
                throw new InvalidOperationException(
                    "Ship build route must be active before units can be queued.");
            }

            _unitRequests[unitRequest.Id] = unitRequest;
            _ui.AddPipeline(unitRequest);
        }
    }
}
