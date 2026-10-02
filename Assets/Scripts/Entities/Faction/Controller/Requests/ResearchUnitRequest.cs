using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Controllers.Factions
{
    public class ResearchUnitRequest : UnitRequest<ResearchType>
    {
        public int Tier { get; }

        public ResearchUnitRequest(FactionData factionData, ResearchType key, int tier) : base(factionData, key)
        {
            Tier = tier;
        }
    }
}
