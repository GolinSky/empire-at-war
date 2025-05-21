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
        IBuildShipChain, IIncomeProvider, IObserver<ISelectionSubject>
    {
        private const float DEFAULT_INCOME = 5f;

        private readonly ISelectionService _selectionService;
        private readonly LazyInject<IPurchaseProcessor> _purchaseMediator;
        private readonly IEconomyProvider _economyProvider;
        private readonly IUiService _uiService;
        private IChainHandler<UnitRequest> _nextChain;
        private ISelectionContext _selectionContext;
        
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
            if(_selectionContext != null)
            {
                _selectionService.RemoveSelectable(_selectionContext);
            }
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

        public void UpdateState(ISelectionSubject selectionSubject)
        {
            if (selectionSubject.UpdatedType == PlayerType.Player)
            {
                _selectionContext = selectionSubject.PlayerSelectionContext;
                Model.SelectionType = _selectionContext.SelectionType;// move it to selection component and reuse it 
            }
        }
    }
}