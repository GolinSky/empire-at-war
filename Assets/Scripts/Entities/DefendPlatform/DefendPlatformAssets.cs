namespace EmpireAtWar.Entities.DefendPlatform
{
    public static class DefendPlatformAssets
    {
        public static string GetDataName(DefendPlatformType type) => type switch
        {
            DefendPlatformType.GolanIII => "AotrGolanIIIDefensePlatformData",
            DefendPlatformType.Empress => "AotrEmpressDefensePlatformData",
            DefendPlatformType.BattleAsteroid => "AsteroidDefendPlatformData",
            DefendPlatformType.QuadLaserSatellite or DefendPlatformType.TurbolaserSatellite or
                DefendPlatformType.IonSatellite or DefendPlatformType.MassDriverSatellite or
                DefendPlatformType.MissileSatellite => type + "Data",
            _ => nameof(DefendPlatformData),
        };

        public static string GetViewName(DefendPlatformType type) => type switch
        {
            DefendPlatformType.GolanIII => "AotrGolanIIIDefensePlatformView",
            DefendPlatformType.Empress => "AotrEmpressDefensePlatformView",
            DefendPlatformType.BattleAsteroid => "AsteroidDefendPlatformView",
            DefendPlatformType.QuadLaserSatellite or DefendPlatformType.TurbolaserSatellite or
                DefendPlatformType.IonSatellite or DefendPlatformType.MassDriverSatellite or
                DefendPlatformType.MissileSatellite => type + "View",
            _ => "DefendPlatformView",
        };
    }
}
