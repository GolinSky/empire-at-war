using EmpireAtWar.Commands.Faction;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Patterns.ChainOfResponsibility;
using EmpireAtWar.Services.NavigationService;
using LightWeightFramework.Controller;
using Zenject;

namespace EmpireAtWar.Controllers.Factions
{
    public interface IBuildShipChain : IChainHandler<UnitRequest>
    {
        
    }
    public class FactionController : Controller<PlayerFactionModel>, IInitializable, ILateDisposable, IFactionCommand, IBuildShipChain
    {
        private readonly INavigationService navigationService;
        private readonly IPurchaseMediator purchaseMediator;
        private IChainHandler<UnitRequest> nextChain;

        public FactionController(PlayerFactionModel model, INavigationService navigationService, IPurchaseMediator purchaseMediator) : base(model)
        {
            this.navigationService = navigationService;
            this.purchaseMediator = purchaseMediator;
            purchaseMediator.Add(this);
        }
        
        public void Initialize()
        {
            navigationService.OnTypeChanged += UpdateType;
        }

        public void LateDispose()
        {
            navigationService.OnTypeChanged -= UpdateType;
        }

        private void UpdateType(SelectionType selectionType)
        {
            Model.SelectionType = selectionType;
        }
        
        public void CloseSelection()
        {
            navigationService.RemoveSelectable();
        }

        public void BuildShip(UnitRequest shipType)
        {
            if (nextChain != null)
            {
                nextChain.Handle(shipType);
            }
        }

        public void TryPurchaseShip(UnitRequest shipType)
        {
            purchaseMediator.Handle(shipType);
        }

        public void RevertBuilding(UnitRequest unitRequest)
        {
            purchaseMediator.RevertFlow(unitRequest);
        }

        public IChainHandler<UnitRequest> SetNext(IChainHandler<UnitRequest> chainHandler)
        {
            nextChain = chainHandler;
            return nextChain;
        }

        public void Handle(UnitRequest unitRequest)
        {
            Model.ShipTypeToBuild = unitRequest;
       
        }
    }
}