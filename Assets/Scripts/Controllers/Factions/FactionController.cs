using EmpireAtWar.Commands.Faction;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Patterns.ChainOfResponsibility;
using EmpireAtWar.Services.NavigationService;
using LightWeightFramework.Controller;
using Zenject;

namespace EmpireAtWar.Controllers.Factions
{
    public interface IBuildShipChain : IChainHandler<ShipType>
    {
        
    }
    public class FactionController : Controller<PlayerFactionModel>, IInitializable, ILateDisposable, IFactionCommand, IBuildShipChain
    {
        private readonly INavigationService navigationService;
        private readonly IPurchaseMediator purchaseMediator;
        private IChainHandler<ShipType> nextChain;

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

        public void BuildShip(ShipType shipType)
        {
            if (nextChain != null)
            {
                nextChain.Handle(shipType);
            }
        }

        public void TryPurchaseShip(ShipType shipType)
        {
            purchaseMediator.Handle(shipType);
        }

        public IChainHandler<ShipType> SetNext(IChainHandler<ShipType> chainHandler)
        {
            nextChain = chainHandler;
            return nextChain;
        }

        public void Handle(ShipType shipType)
        {
            Model.ShipTypeToBuild = shipType;
        }
    }
}