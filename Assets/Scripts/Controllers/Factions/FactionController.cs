using EmpireAtWar.Commands.Faction;
using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Patterns.ChainOfResponsibility;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Ui.Base;
using LightWeightFramework.Controller;
using Zenject;

namespace EmpireAtWar.Controllers.Factions
{
    public class FactionController : Controller<PlayerFactionModel>, IInitializable, ILateDisposable, IFactionCommand,
        IBuildShipChain, IIncomeProvider, IObserver<ISelectionContext>
    {
        private const float DEFAULT_INCOME = 5f;

        private readonly ISelectionService _selectionService;
        private readonly LazyInject<IPurchaseProcessor> _purchaseMediator;
        private readonly IEconomyProvider _economyProvider;
        private readonly IUiService _uiService;
        private IChainHandler<UnitRequest> _nextChain;
        public float Income { get; private set; }

        public FactionController(
            PlayerFactionModel model,
            ISelectionService selectionService,
            LazyInject<IPurchaseProcessor> purchaseMediator,
            IEconomyProvider economyProvider,
            IUiService uiService) : base(model)
        {
            Income = DEFAULT_INCOME;
            _selectionService = selectionService;
            _purchaseMediator = purchaseMediator;
            _economyProvider = economyProvider;
            _uiService = uiService;
        }

        public void Initialize()
        {
            _purchaseMediator.Value.Add(this);
            _selectionService.AddObserver(this);
            _economyProvider.AddProvider(this);
            _uiService.CreateUi(UiType.Faction);
        }

        public void LateDispose()
        {
            _selectionService.RemoveObserver(this);
            _economyProvider.RemoveProvider(this);
        }

        public void CloseSelection()
        {
            _selectionService.RemoveSelectable();
        }

        public void BuildUnit(UnitRequest unitRequest)
        {
            switch (unitRequest)
            {
                case LevelUnitRequest levelUnitRequest:
                    Model.CurrentLevel++;
                    Income = DEFAULT_INCOME * Model.CurrentLevel;
                    _economyProvider.RecalculateIncome(this);
                    break;
            }

            if (_nextChain != null)
            {
                _nextChain.Handle(unitRequest);
            }
        }

        public void TryPurchaseUnit(UnitRequest unitRequest)
        {
            _purchaseMediator.Value.Handle(unitRequest);
        }

        public void RevertBuilding(UnitRequest unitRequest)
        {
            _purchaseMediator.Value.RevertFlow(unitRequest);
        }

        public IChainHandler<UnitRequest> SetNext(IChainHandler<UnitRequest> chainHandler)
        {
            _nextChain = chainHandler;
            return _nextChain;
        }

        public void Handle(UnitRequest unitRequest)
        {
            Model.UnitToBuild = unitRequest;
        }

        public void UpdateState(ISelectionContext value)
        {
            Model.SelectionType = value.SelectionType;// move it to selection component and reuse it 
        }
    }
}