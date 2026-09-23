using EmpireAtWar.Mvc;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Commands.ShipUi
{
    public interface IShipUiCommand : ICommand
    {
        void CloseSelection();
        void SelectShipGroup(ShipType shipType);
    }
}
