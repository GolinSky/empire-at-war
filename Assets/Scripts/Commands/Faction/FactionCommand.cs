using EmpireAtWar.Models.Factions;
using LightWeightFramework.Command;

namespace EmpireAtWar.Commands.Faction
{
    public interface IFactionCommand:ICommand
    {
        void CloseSelection();
        void BuildShip(ShipType shipType);
        void TryPurchaseShip(ShipType shipType);
        void RevertBuilding(string id);
    }
}