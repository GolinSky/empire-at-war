using EmpireAtWar.Controllers.Factions;
using LightWeightFramework.Command;

namespace EmpireAtWar.Commands.Faction
{
    public interface IFactionCommand:ICommand
    {
        void CloseSelection();
        void BuildUnit(UnitRequest shipType);
        void TryPurchaseUnit(UnitRequest shipType);
        void RevertBuilding(UnitRequest id);
    }
}