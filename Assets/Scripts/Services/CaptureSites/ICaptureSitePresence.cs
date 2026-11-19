using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.CaptureSites
{
    public interface ICaptureSitePresence
    {
        (float Friendly, float Hostile) GetTeamStrengths(ICaptureSite site, PlayerId owner);

        bool HasHostileUnits(ICaptureSite site, PlayerId owner);
    }
}
