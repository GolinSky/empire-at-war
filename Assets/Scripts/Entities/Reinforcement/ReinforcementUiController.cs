using System;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Services.Reinforcement;
using EmpireAtWar.Ui.Base;
using EmpireAtWar.Views.Reinforcement;
using Zenject;

namespace EmpireAtWar.Presenters.Reinforcement
{
    public class ReinforcementUiController : IReinforcementPresenter, IInitializable, ILateDisposable
    {
        private readonly IUiService _uiService;
        private readonly IReinforcementService _reinforcementService;
        private readonly ReinforcementModel _model;
        private readonly ReinforcementData _data;

        private IReinforcementUi _ui;

        public ReinforcementUiController(
            IUiService uiService,
            IReinforcementService reinforcementService,
            ReinforcementModel model,
            ReinforcementData data)
        {
            _uiService = uiService;
            _reinforcementService = reinforcementService;
            _model = model;
            _data = data;
        }

        public void Initialize()
        {
            BaseUi ui = _uiService.CreateUi(UiType.Reinforcement);
            _ui = ui as IReinforcementUi
                ?? throw new InvalidOperationException("The reinforcement prefab does not implement IReinforcementUi.");

            _ui.SetModel(_model);
            _ui.SetPresenter(this);
            _ui.SetData(_data);
            _ui.Initialize();
        }

        public void LateDispose()
        {
            _ui?.Dispose();
        }

        public void TrySpawnReinforcement(string id)
        {
            _reinforcementService.TrySpawnReinforcement(id);
        }
    }
}
