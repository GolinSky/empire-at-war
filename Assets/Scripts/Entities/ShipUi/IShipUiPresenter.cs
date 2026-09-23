using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.ShipAbilities;

namespace EmpireAtWar.Presenters.ShipUi
{
    public interface IShipUiPresenter
    {
        void CloseSelection();
        void SelectShipGroup(ShipType shipType);
        void PressAbility(ShipAbilityId id);
    }
}
