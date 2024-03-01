using EmpireAtWar.Models.Factions;
using LightWeightFramework.Command;

namespace EmpireAtWar.Commands.Faction
{
    public interface IFactionCommand:ICommand
    {
        void CloseSelection();
        void BuildShip(ShipType shipType);
        bool TryPurchaseShip(ShipType shipType);
    }
}