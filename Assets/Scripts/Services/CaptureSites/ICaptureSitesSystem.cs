using EmpireAtWar.Models.Factions;
using UnityEngine;

namespace EmpireAtWar.Services.CaptureSites
{
    public interface ICaptureSitesSystem
    {
        bool IsPositionInAnySite(Vector3 position, float clearance = 0f);
        bool TryGetCaptureTarget(PlayerType playerType, Vector3 origin, out Vector3 position);
        bool TryBuildOnOwnedSite(PlayerType playerType);
    }
}
