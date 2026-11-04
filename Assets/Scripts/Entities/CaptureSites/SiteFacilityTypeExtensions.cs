using System;
using EmpireAtWar.Entities.DefendPlatform;

namespace EmpireAtWar.Entities.CaptureSites
{
    public static class SiteFacilityTypeExtensions
    {
        public static DefendPlatformType ToSatelliteType(this SiteFacilityType type) => type switch
        {
            SiteFacilityType.QuadLaserSatellite => DefendPlatformType.QuadLaserSatellite,
            SiteFacilityType.TurbolaserSatellite => DefendPlatformType.TurbolaserSatellite,
            SiteFacilityType.IonSatellite => DefendPlatformType.IonSatellite,
            SiteFacilityType.MassDriverSatellite => DefendPlatformType.MassDriverSatellite,
            SiteFacilityType.MissileSatellite => DefendPlatformType.MissileSatellite,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}
