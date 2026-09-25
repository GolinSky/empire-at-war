using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Controllers.Factions
{
    public class ResearchUnitRequest : UnitRequest<ResearchType>
    {
        public ResearchUnitRequest(FactionData factionData, ResearchType key) : base(factionData, key)
        {
        }
    }
}
