using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.MiningFacility;

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

    public class LevelUnitRequest : UnitRequest
    {
        public int Level { get; }
        public override string Id { get; }

        public LevelUnitRequest(FactionData factionData, int level) : base(factionData)
        {
            Level = level;
            Id = level.ToString();
        }
    }

    public class MiningFacilityUnitRequest : UnitRequest
    {
        public MiningFacilityType MiningFacilityType { get; }
        public override string Id => MiningFacilityType.ToString();

        public MiningFacilityUnitRequest(FactionData factionData, MiningFacilityType miningFacilityType) : base(factionData)
        {
            MiningFacilityType = miningFacilityType;
        }

    }
}