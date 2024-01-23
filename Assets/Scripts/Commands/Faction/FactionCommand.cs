using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Views.Ship;
using WorkShop.LightWeightFramework.Command;

namespace EmpireAtWar.Commands.Faction
{
    public interface IFactionCommand:ICommand
    {
        void CloseSelection();

        void BuildShip(ShipType shipType);
    }
    
    public class FactionCommand:Command<FactionController>, IFactionCommand
    {
        private readonly INavigationService navigationService;
        private readonly ShipFacadeFactory shipFacadeFactory;

        public FactionCommand(FactionController controller, INavigationService navigationService, ShipFacadeFactory shipFacadeFactory) : base(controller)
        {
            this.navigationService = navigationService;
            this.shipFacadeFactory = shipFacadeFactory;
        }

        public void CloseSelection()
        {
            navigationService.RemoveSelectable();
        }

        public void BuildShip(ShipType shipType)
        {
            shipFacadeFactory.Create(shipType);
        }
    }
}