using EmpireAtWar.Mvc;

namespace EmpireAtWar.Commands.ShipUi
{
    public interface IShipUiCommand : ICommand
    {
        void CloseSelection();
    }
}
