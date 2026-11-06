using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.CaptureSites
{
    public interface ICaptureSiteConstruction
    {
        bool TryBuildOnOwnedSite(PlayerId owner);
        bool TryStartConstruction(int siteIndex, PlayerId owner, SiteFacilityType type);
    }
}
