using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Models.DefendPlatform;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Controllers.Factions
{
    public interface IUnitRequestFactory
    {
        ShipUnitRequest ConstructUnitRequest(FactionData factionData, ShipType shipType);
        LevelUnitRequest ConstructUnitRequest(FactionData factionData, int level);
        MiningFacilityUnitRequest ConstructUnitRequest(FactionData factionData, MiningFacilityType miningFacilityType);
        DefendPlatformUnitRequest ConstructUnitRequest(FactionData factionData, DefendPlatformType platformType);
    }

    public class UnitRequestFactory : IUnitRequestFactory
    {
        public ShipUnitRequest ConstructUnitRequest(FactionData factionData, ShipType shipType)
        {
            return new ShipUnitRequest(factionData, shipType);
        }

        public LevelUnitRequest ConstructUnitRequest(FactionData factionData, int level)
        {
            return new LevelUnitRequest(factionData, level);
        }

        public MiningFacilityUnitRequest ConstructUnitRequest(FactionData factionData, MiningFacilityType miningFacilityType)
        {
            return new MiningFacilityUnitRequest(factionData, miningFacilityType);
        }

        public DefendPlatformUnitRequest ConstructUnitRequest(FactionData factionData, DefendPlatformType platformType)
        {
            return new DefendPlatformUnitRequest(factionData, platformType);
        }
    }
}