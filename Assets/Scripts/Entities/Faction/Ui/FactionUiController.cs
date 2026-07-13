using System;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Factions;
using EmpireAtWar.Ui.Base;
using EmpireAtWar.Views.Factions;
using Zenject;

namespace EmpireAtWar.Presenters.Factions
{
    public class FactionUiController : IFactionPresenter, IInitializable, ILateDisposable
    {
        private readonly IUiService _uiService;
        private readonly IFactionService _factionService;
        private readonly IPlayerFactionModelObserver _model;
        private readonly PlayerFactionData _data;
        private readonly IUnitRequestFactory _unitRequestFactory;

        private IFactionUi _ui;

        public FactionUiController(
            IUiService uiService,
            IFactionService factionService,
            IPlayerFactionModelObserver model,
            PlayerFactionData data,
            IUnitRequestFactory unitRequestFactory)
        {
            _uiService = uiService;
            _factionService = factionService;
            _model = model;
            _data = data;
            _unitRequestFactory = unitRequestFactory;
        }

        public void Initialize()
        {
            BaseUi ui = _uiService.CreateUi(UiType.Faction);
            _ui = ui as IFactionUi
                ?? throw new InvalidOperationException("The faction prefab does not implement IFactionUi.");

            _ui.SetModel(_model);
            _ui.SetPresenter(this);
            _ui.SetData(_data);
            _ui.SetUnitRequestFactory(_unitRequestFactory);
            _ui.Initialize();
        }

        public void LateDispose()
        {
            _ui?.Dispose();
        }

        public void ChangeSelection()
        {
            _factionService.ChangeSelection();
        }

        public void CloseSelection()
        {
            _factionService.CloseSelection();
        }

        public void BuildUnit(UnitRequest unitRequest)
        {
            _factionService.BuildUnit(unitRequest);
        }

        public void TryPurchaseUnit(UnitRequest unitRequest)
        {
            _factionService.TryPurchaseUnit(unitRequest);
        }

        public void RevertBuilding(UnitRequest unitRequest)
        {
            _factionService.RevertBuilding(unitRequest);
        }
    }
}
