using EmpireAtWar.Models.Factions;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Services.ShipAbilities;

namespace EmpireAtWar.Presenters.ShipUi
{
    public interface IShipUiPresenter
    {
        void CloseSelection();
        void SelectShipGroup(ShipType shipType);
        void SelectSquadronGroup(SquadronType squadronType);
        void PressAbility(ShipAbilityId id);
    }
}
