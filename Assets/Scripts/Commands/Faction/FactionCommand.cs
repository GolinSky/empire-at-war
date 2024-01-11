using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Views.Ship;
using WorkShop.LightWeightFramework.Command;
using WorkShop.LightWeightFramework.Game;

namespace EmpireAtWar.Commands.Faction
{
    public interface IFactionCommand:ICommand
    {
        void CloseSelection();

        void BuildShip(RepublicShipType republicShipType);
    }
    
    public class FactionCommand:Command<FactionController>, IFactionCommand
    {
        private readonly INavigationService navigationService;
        private readonly ShipView.ShipFactory shipFactory;

        public FactionCommand(FactionController entity, IGameObserver gameObserver, INavigationService navigationService, ShipView.ShipFactory shipFactory) : base(entity, gameObserver)
        {
            this.navigationService = navigationService;
            this.shipFactory = shipFactory;
        }

        public void CloseSelection()
        {
            navigationService.RemoveSelectable();
        }

        public void BuildShip(RepublicShipType republicShipType)
        {
            shipFactory.Create(republicShipType);
        }
    }
}