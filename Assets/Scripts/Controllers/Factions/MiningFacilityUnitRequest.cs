using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.MiningFacility;

namespace EmpireAtWar.Controllers.Factions
{
    public class MiningFacilityUnitRequest : UnitRequest<MiningFacilityType>
    {
        public MiningFacilityUnitRequest(FactionData factionData, MiningFacilityType key) : base(factionData, key)
        {
        }
    }
}