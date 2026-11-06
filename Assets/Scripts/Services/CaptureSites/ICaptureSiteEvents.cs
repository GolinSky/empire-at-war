using System;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.CaptureSites
{
    public interface ICaptureSiteEvents
    {
        event Action<PlayerId, PlayerId, SiteFacilityType> SiteCaptured;
        event Action<PlayerId, SiteFacilityType> SiteLost;
        event Action<PlayerId, SiteFacilityType> ConstructionStarted;
        event Action<PlayerId, SiteFacilityType> ConstructionCompleted;
    }
}
