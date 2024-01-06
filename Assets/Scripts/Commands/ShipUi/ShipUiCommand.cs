using EmpireAtWar.Controllers.ShipUi;
using WorkShop.LightWeightFramework.Command;
using WorkShop.LightWeightFramework.Game;

namespace EmpireAtWar.Commands.ShipUi
{
    public interface IShipUiCommand : ICommand
    {
        void CloseSelection();
    }

    public class ShipUiCommand : Command<ShipUiController>, IShipUiCommand
    {
        public ShipUiCommand(ShipUiController controller, IGameObserver gameObserver) : base(controller, gameObserver)
        {
        }

        public void CloseSelection()
        {
            Entity.CloseSelection();
        }
    }
}