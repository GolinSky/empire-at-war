using System.Collections.Generic;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.CaptureSites
{
    public interface ICaptureSitesSystem
    {
        IReadOnlyList<ICaptureSite> Sites { get; }
        bool IsPositionInAnySite(Vector3 position, float clearance = 0f);
        bool TryGetCaptureTarget(PlayerId owner, Vector3 origin, out Vector3 position);
        bool TryGetThreatenedSite(PlayerId owner, out Vector3 position);
        bool TryGetRaidTarget(PlayerId attacker, Vector3 origin, out Vector3 position);
        bool TryBuildOnOwnedSite(PlayerId owner);
    }
}
