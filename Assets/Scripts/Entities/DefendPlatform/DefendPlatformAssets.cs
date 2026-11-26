namespace EmpireAtWar.Entities.DefendPlatform
{
    public static class DefendPlatformAssets
    {
        public static string GetDataName(DefendPlatformType type) => type switch
        {
            DefendPlatformType.GolanIII => "AotrGolanIIIDefensePlatformData",
            DefendPlatformType.Empress => "AotrEmpressDefensePlatformData",
            DefendPlatformType.BattleAsteroid => "AsteroidDefendPlatformData",
            DefendPlatformType.Xq6Repair => "Xq6RepairPlatformData",
            DefendPlatformType.QuadLaserSatellite or DefendPlatformType.TurbolaserSatellite or
                DefendPlatformType.IonSatellite or DefendPlatformType.MassDriverSatellite or
                DefendPlatformType.MissileSatellite or DefendPlatformType.RepairSatellite => type + "Data",
            _ => nameof(DefendPlatformData),
        };

        public static string GetViewName(DefendPlatformType type) => type switch
        {
            DefendPlatformType.GolanIII => "AotrGolanIIIDefensePlatformView",
            DefendPlatformType.Empress => "AotrEmpressDefensePlatformView",
            DefendPlatformType.BattleAsteroid => "AsteroidDefendPlatformView",
            DefendPlatformType.Xq6Repair => "Xq6RepairPlatformView",
            DefendPlatformType.QuadLaserSatellite or DefendPlatformType.TurbolaserSatellite or
                DefendPlatformType.IonSatellite or DefendPlatformType.MassDriverSatellite or
                DefendPlatformType.MissileSatellite or DefendPlatformType.RepairSatellite => type + "View",
            _ => "DefendPlatformView",
        };
    }
}
