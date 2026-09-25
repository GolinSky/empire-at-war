using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Controllers.Factions
{
    public class SquadronUnitRequest : UnitRequest<SquadronType>
    {
        public SquadronUnitRequest(FactionData factionData, SquadronType key) : base(factionData, key)
        {
        }
    }
}
