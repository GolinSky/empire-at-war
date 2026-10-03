using EmpireAtWar.Entities.CaptureSites;
using UnityEngine;

namespace EmpireAtWar.Entities.Map
{
    public readonly struct SiteSpot
    {
        public Vector3 Center { get; }
        public SiteFacilityType FacilityType { get; }

        public SiteSpot(Vector3 center, SiteFacilityType facilityType)
        {
            Center = center;
            FacilityType = facilityType;
        }
    }
}
