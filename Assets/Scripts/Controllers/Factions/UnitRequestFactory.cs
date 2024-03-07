using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Controllers.Factions
{
    public interface IUnitRequestFactory
    {
        ShipUnitRequest ConstructUnitRequest(FactionData factionData, ShipType shipType);
        LevelUnitRequest ConstructUnitRequest(FactionData factionData, int level);
    }
    
    public class UnitRequestFactory:IUnitRequestFactory
    {
        public ShipUnitRequest ConstructUnitRequest(FactionData factionData, ShipType shipType)
        {
            return new ShipUnitRequest(factionData, shipType);
        }

        public LevelUnitRequest ConstructUnitRequest(FactionData factionData, int level)
        {
            return new LevelUnitRequest(factionData, level);
        }
    }
}