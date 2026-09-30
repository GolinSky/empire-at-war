using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Entities.CaptureSites
{
    public interface ICaptureSite
    {
        PlayerId Owner { get; }
        CaptureSiteState State { get; }
        SiteFacilityType FacilityType { get; }
        float CaptureProgress { get; }
        float ConstructionProgress { get; }
        Vector3 Center { get; }
        bool IsRevealed { get; }
        bool IsOperational { get; }

        bool Contains(Vector3 position, float clearance = 0f);
    }
}
