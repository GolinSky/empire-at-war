using EmpireAtWar.Commands.Faction;
using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Patterns.ChainOfResponsibility;
using EmpireAtWar.Services.EconomyMediator;
using EmpireAtWar.Services.NavigationService;
using LightWeightFramework.Controller;
using Zenject;

namespace EmpireAtWar.Controllers.Factions
{
    public class FactionController : Controller<PlayerFactionModel>, IInitializable, ILateDisposable, IFactionCommand,
        IBuildShipChain, IIncomeProvider
    {
        private const float DefaultIncome = 5f;

        private readonly INavigationService navigationService;
        private readonly LazyInject<IPurchaseMediator> purchaseMediator;
        private readonly IEconomyProvider economyProvider;
        private IChainHandler<UnitRequest> nextChain;
        public float Income { get; private set; }

        public FactionController(
            PlayerFactionModel model,
            INavigationService navigationService,
            [Inject(Id = PlayerType.Player)] LazyInject<IPurchaseMediator> purchaseMediator,
            IEconomyMediator economyMediator) : base(model)
        {
            Income = DefaultIncome;
            this.navigationService = navigationService;
            this.purchaseMediator = purchaseMediator;
            economyProvider = economyMediator.GetProvider(PlayerType.Player);
        }

        public void Initialize()
        {
            purchaseMediator.Value.Add(this);

            navigationService.OnTypeChanged += UpdateType;
            economyProvider.AddProvider(this);
        }

        public void LateDispose()
        {
            navigationService.OnTypeChanged -= UpdateType;
            economyProvider.RemoveProvider(this);
        }

        private void UpdateType(SelectionType selectionType)
        {
            Model.SelectionType = selectionType;
        }

        public void CloseSelection()
        {
            navigationService.RemoveSelectable();
        }

        public void BuildUnit(UnitRequest unitRequest)
        {
            switch (unitRequest)
            {
                case LevelUnitRequest levelUnitRequest:
                    Model.CurrentLevel++;
                    Income = DefaultIncome * Model.CurrentLevel;
                    economyProvider.RecalculateIncome(this);
                    break;
            }

            if (nextChain != null)
            {
                nextChain.Handle(unitRequest);
            }
        }

        public void TryPurchaseUnit(UnitRequest unitRequest)
        {
            purchaseMediator.Value.Handle(unitRequest);
        }

        public void RevertBuilding(UnitRequest unitRequest)
        {
            purchaseMediator.Value.RevertFlow(unitRequest);
        }

        public IChainHandler<UnitRequest> SetNext(IChainHandler<UnitRequest> chainHandler)
        {
            nextChain = chainHandler;
            return nextChain;
        }

        public void Handle(UnitRequest unitRequest)
        {
            Model.UnitToBuild = unitRequest;
        }
    }
}