using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.CaptureSites
{
    /// <summary>Stands in for the capture sites when the battle has none.</summary>
    public sealed class NoCaptureSites : ICaptureSitesSystem
    {
        public event Action<PlayerId, PlayerId, SiteFacilityType> SiteCaptured { add { } remove { } }
        public event Action<PlayerId, SiteFacilityType> SiteLost { add { } remove { } }
        public event Action<PlayerId, SiteFacilityType> ConstructionStarted { add { } remove { } }
        public IReadOnlyList<ICaptureSite> Sites => Array.Empty<ICaptureSite>();

        public bool IsPositionInAnySite(Vector3 position, float clearance = 0f) => false;

        public bool TryGetCaptureTarget(PlayerId owner, Vector3 origin, out Vector3 position)
        {
            position = default;
            return false;
        }

        public bool TryGetThreatenedSite(PlayerId owner, out Vector3 position)
        {
            position = default;
            return false;
        }

        public bool TryGetRaidTarget(PlayerId attacker, Vector3 origin, out Vector3 position)
        {
            position = default;
            return false;
        }

        public bool TryBuildOnOwnedSite(PlayerId owner) => false;
    }
}
