using System;
using EmpireAtWar.Entities.CaptureSites;
using UnityEngine;

namespace EmpireAtWar.Services.CaptureSites
{
    /// <summary>Pays for and spawns capture-site facilities for one side.</summary>
    public interface ISiteFacilityBuilder
    {
        bool CanAfford(float price);

        bool TrySpend(float price);

        /// <returns>The facility's entity id.</returns>
        long Build(SiteFacilityType facilityType, Vector3 position, Action onDestroyed);
    }
}
