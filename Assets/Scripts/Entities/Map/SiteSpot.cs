using EmpireAtWar.Entities.CaptureSites;
using UnityEngine;

namespace EmpireAtWar.Entities.Map
{
    public readonly struct SiteSpot
    {
        public SiteSpot(Vector3 center, SiteFacilityType facilityType)
        {
            Center = center;
            FacilityType = facilityType;
        }

        public Vector3 Center { get; }
        public SiteFacilityType FacilityType { get; }
    }
}
