using System;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Ui.Base;
using EmpireAtWar.Views.Economy;
using Zenject;

namespace EmpireAtWar.Presenters.Economy
{
    public class EconomyUiController : IInitializable, ILateDisposable
    {
        private readonly IUiService _uiService;
        private readonly IEconomyModelObserver _model;

        private IEconomyUi _ui;

        public EconomyUiController(IUiService uiService, IEconomyModelObserver model)
        {
            _uiService = uiService;
            _model = model;
        }

        public void Initialize()
        {
            BaseUi ui = _uiService.CreateUi(UiType.Economy);
            _ui = ui as IEconomyUi
                ?? throw new InvalidOperationException("The economy prefab does not implement IEconomyUi.");

            _ui.SetModel(_model);
            _ui.Initialize();
        }

        public void LateDispose()
        {
            _ui?.Dispose();
        }
    }
}
