using EmpireAtWar.Models.Factions;
using WorkShop.LightWeightFramework.Command;

namespace EmpireAtWar.Commands.Faction
{
    public interface IFactionCommand:ICommand
    {
        void CloseSelection();
        void BuildShip(ShipType shipType);
    }
}