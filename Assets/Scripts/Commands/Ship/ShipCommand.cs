using EmpireAtWar.Controllers.Ship;
using EmpireAtWar.Services.NavigationService;
using WorkShop.LightWeightFramework.Command;
using WorkShop.LightWeightFramework.Game;
using Zenject;

namespace EmpireAtWar.Commands.Ship
{
    public interface IShipCommand:ICommand
    {
    }
    public class ShipCommand: Command<ShipEntity>, IShipCommand, IInitializable
    {

        public ShipCommand(ShipEntity entity, IGameObserver gameObserver, INavigationService navigationService) : base(entity, gameObserver)
        {
            AddCommand(
                new SelectionCommand(entity, navigationService, entity)
            );
        }
        
        public void Initialize()
        {
        }
    }
}