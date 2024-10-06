using EmpireAtWar.Models.DefendPlatform;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Controllers.Factions
{
    public class DefendPlatformUnitRequest:UnitRequest<DefendPlatformType>
    {
        public DefendPlatformUnitRequest(FactionData factionData, DefendPlatformType key) : base(factionData, key)
        {
        }
    }
}