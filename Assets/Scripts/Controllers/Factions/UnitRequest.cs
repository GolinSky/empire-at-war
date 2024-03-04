using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Controllers.Factions
{
    public abstract class UnitRequest
    {
        public FactionData FactionData { get; }
        public abstract string Id { get;  }

        protected UnitRequest(FactionData factionData)
        {
            FactionData = factionData;
        }
    }


    public class ShipUnitRequest: UnitRequest
    {
        public ShipType ShipType { get; }
        public override string Id => ShipType.ToString();

        public ShipUnitRequest(FactionData factionData, ShipType shipType) : base(factionData)
        {
            ShipType = shipType;
        }

    }
}