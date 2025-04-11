using EmpireAtWar.Commands.Faction;
using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Patterns.ChainOfResponsibility;
using EmpireAtWar.Services.NavigationService;
using LightWeightFramework.Controller;
using Zenject;

namespace EmpireAtWar.Controllers.Factions
{
    public class FactionController : Controller<PlayerFactionModel>, IInitializable, ILateDisposable, IFactionCommand,
        IBuildShipChain, IIncomeProvider, ITickable
    {
        private const float DEFAULT_INCOME = 5f;

        private readonly INavigationService _navigationService;
        private readonly LazyInject<IPurchaseProcessor> _purchaseMediator;
        private readonly IEconomyProvider _economyProvider;
        private IChainHandler<UnitRequest> _nextChain;
        public float Income { get; private set; }

        public FactionController(
            PlayerFactionModel model,
            INavigationService navigationService,
            LazyInject<IPurchaseProcessor> purchaseMediator,
            IEconomyProvider economyProvider) : base(model)
        {
            Income = DEFAULT_INCOME;
            _navigationService = navigationService;
            _purchaseMediator = purchaseMediator;
            _economyProvider = economyProvider;
        }

        public void Initialize()
        {
            _purchaseMediator.Value.Add(this);

            _navigationService.OnTypeChanged += UpdateType;
            _economyProvider.AddProvider(this);
        }

        public void LateDispose()
        {
            _navigationService.OnTypeChanged -= UpdateType;
            _economyProvider.RemoveProvider(this);
        }

        private void UpdateType(SelectionType selectionType)
        {
            Model.SelectionType = selectionType;
        }

        public void CloseSelection()
        {
            _navigationService.RemoveSelectable();
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

        public void Tick()
        {
            
        }
    }
}