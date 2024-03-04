using EmpireAtWar.Controllers.Factions;
using LightWeightFramework.Command;

namespace EmpireAtWar.Commands.Faction
{
    public interface IFactionCommand:ICommand
    {
        void CloseSelection();
        void BuildShip(UnitRequest shipType);
        void TryPurchaseShip(UnitRequest shipType);
        void RevertBuilding(UnitRequest id);
    }
}