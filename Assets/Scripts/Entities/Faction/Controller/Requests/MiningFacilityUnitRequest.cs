using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Controllers.Factions
{
    public class MiningFacilityUnitRequest : UnitRequest<MiningFacilityType>
    {
        public MiningFacilityUnitRequest(FactionData factionData, MiningFacilityType key) : base(factionData, key)
        {
        }
    }
}