using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Controllers.Factions
{
    public class ShipUnitRequest: UnitRequest<ShipType>
    {
        public ShipUnitRequest(FactionData factionData, ShipType key) : base(factionData, key)
        {
        }
    }
}