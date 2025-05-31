using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Controllers.Factions
{
    public class LevelUnitRequest : UnitRequest<int>
    {
        public LevelUnitRequest(FactionData factionData, int key) : base(factionData, key)
        {
        }
    }
}