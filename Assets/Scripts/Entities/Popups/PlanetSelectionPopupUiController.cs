using System;
using EmpireAtWar.Entities.Planet;
using UnityEngine;
using EmpireAtWar.Entities.MainMenu.Skirmish;
using EmpireAtWar.Services.Popups;
using EmpireAtWar.Ui.Base;

namespace EmpireAtWar.Entities.Popups
{
    internal sealed class PlanetSelectionPopupUiController : IPopupController
    {
        private readonly IUiService _uiService;
        private readonly ISkirmishModelObserver _model;
        private readonly ISkirmishPresenter _presenter;
        private readonly Action _closed;
        private readonly Func<PlanetType, Texture2D> _getPlanetPreview;
        private PlanetSelectionPopupUi _ui;
        private Action _complete;

        public PlanetSelectionPopupUiController(IUiService uiService, ISkirmishModelObserver model,
            ISkirmishPresenter presenter, Action closed, Func<PlanetType, Texture2D> getPlanetPreview)
        {
            _uiService = uiService;
            _model = model;
            _presenter = presenter;
            _closed = closed;
            _getPlanetPreview = getPlanetPreview;
        }

        public BaseUi Open(Action complete)
        {
            _complete = complete;
            _ui = _uiService.CreateUi<PlanetSelectionPopupUi>();
            _ui.PlanetSelected += SelectPlanet;
            _ui.CloseRequested += Cancel;
            _ui.Initialize(_model, _getPlanetPreview);
            _ui.Show();
            return _ui;
        }

        public void SetCovered(bool covered) => _ui.SetCovered(covered);
        public void Cancel() => _complete();

        public void Dispose(bool dismissed)
        {
            _ui.PlanetSelected -= SelectPlanet;
            _ui.CloseRequested -= Cancel;
            _ui.Dispose();
            if (!dismissed) _closed();
        }

        private void SelectPlanet(int index)
        {
            if (_model.CanEditSettings) _presenter.SelectPlanet(index);
            _complete();
        }
    }
}
