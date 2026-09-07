using System;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Services.Reinforcement;
using EmpireAtWar.Services.UiRouting;
using EmpireAtWar.Ui.Base;
using EmpireAtWar.Views.Reinforcement;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Presenters.Reinforcement
{
    public class ReinforcementUiController : IReinforcementPresenter,
        IInitializable, ILateDisposable, ISkirmishUiRoute
    {
        private readonly IUiService _uiService;
        private readonly IReinforcementService _reinforcementService;
        private readonly ReinforcementModel _model;
        private readonly ReinforcementData _data;
        private readonly ISkirmishRouteNavigation _routeNavigation;

        private IReinforcementUi _ui;

        public ReinforcementUiController(
            IUiService uiService,
            IReinforcementService reinforcementService,
            ReinforcementModel model,
            ReinforcementData data,
            ISkirmishRouteNavigation routeNavigation)
        {
            _uiService = uiService ??
                throw new ArgumentNullException(nameof(uiService));
            _reinforcementService = reinforcementService ??
                throw new ArgumentNullException(nameof(reinforcementService));
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _routeNavigation = routeNavigation ??
                throw new ArgumentNullException(nameof(routeNavigation));
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
                _ui.Dispose();
            }
        }

        public void TrySpawnReinforcement(string id)
        {
            _reinforcementService.TrySpawnReinforcement(id);
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
            if (_ui == null)
            {
                BaseUi ui = _uiService.CreateUi(
                    UiType.Reinforcement,
                    parentTransform);
                _ui = ui as IReinforcementUi
                    ?? throw new InvalidOperationException(
                        "The reinforcement prefab does not implement IReinforcementUi.");

                _ui.SetModel(_model);
                _ui.SetPresenter(this);
                _ui.SetData(_data);
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
