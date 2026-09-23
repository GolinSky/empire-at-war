using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Presenters.ShipUi
{
    public interface IShipUiPresenter
    {
        void CloseSelection();
        void SelectShipGroup(ShipType shipType);
    }
}
