using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.PlayerCommands
{
    public interface ISiteConstructionCommandHandler
    {
        /// <summary>Builds on a site owned by the payer or an ally, paid by the payer.</summary>
        bool TryStartAlliedConstruction(int siteIndex, PlayerId payer, SiteFacilityType facilityType);
    }
}
