using EmpireAtWar.Controllers.ShipUi;
using WorkShop.LightWeightFramework.Command;

namespace EmpireAtWar.Commands.ShipUi
{
    public interface IShipUiCommand : ICommand
    {
        void CloseSelection();
    }

    public class ShipUiCommand : Command<ShipUiController>, IShipUiCommand
    {
        public ShipUiCommand(ShipUiController controller) : base(controller)
        {
        }

        public void CloseSelection()
        {
            Controller.CloseSelection();
        }
    }
}