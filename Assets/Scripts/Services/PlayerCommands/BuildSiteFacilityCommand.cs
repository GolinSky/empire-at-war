using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.PlayerCommands
{
    public readonly struct BuildSiteFacilityCommand
    {
        public PlayerId Issuer { get; }
        public int SiteIndex { get; }
        public SiteFacilityType FacilityType { get; }

        public BuildSiteFacilityCommand(PlayerId issuer, int siteIndex, SiteFacilityType facilityType)
        {
            Issuer = issuer;
            SiteIndex = siteIndex;
            FacilityType = facilityType;
        }
    }
}
