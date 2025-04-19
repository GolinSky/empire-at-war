using LightWeightFramework.Command;

namespace EmpireAtWar.Commands.ShipUi
{
    public interface IShipUiCommand : ICommand
    {
        void CloseSelection();
        void MoveToPosition();
    }
}