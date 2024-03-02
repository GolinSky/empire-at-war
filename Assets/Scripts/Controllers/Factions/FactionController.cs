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
        private readonly IPurchaseFlow purchaseFlow;

        public FactionController(PlayerFactionModel model, INavigationService navigationService, IPurchaseFlow purchaseFlow) : base(model)
        {
            this.navigationService = navigationService;
            this.purchaseFlow = purchaseFlow;
            purchaseFlow.Add(this);
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
            if (next != null)
            {
                next.Handle(shipType);
            }
        }

        private IChainHandler<ShipType> next;
        public void TryPurchaseShip(ShipType shipType)
        {
            purchaseFlow.Handle(shipType);
        }

        public IChainHandler<ShipType> SetNext(IChainHandler<ShipType> chainHandler)
        {
            next = chainHandler;
            return next;
        }

        public void Handle(ShipType shipType)
        {
            Model.ShipTypeToBuild = shipType;
        }
    }
}