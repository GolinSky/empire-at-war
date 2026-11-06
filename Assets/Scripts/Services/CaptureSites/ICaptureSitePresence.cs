using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.CaptureSites
{
    public interface ICaptureSitePresence
    {
        bool HasHostileUnits(ICaptureSite site, PlayerId owner);
    }
}
