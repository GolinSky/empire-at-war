using System;

namespace EmpireAtWar.Entities.MiningFacility
{
    public static class MiningFacilityAssets
    {
        public static string GetName(MiningFacilityType type) => type switch
        {
            MiningFacilityType.CommonMiner => "MiningFacility",
            MiningFacilityType.BattleMiner => "MiningFacility",
            MiningFacilityType.AsteroidMiner => "AsteroidMiningFacility",
            MiningFacilityType.TibannaGasStation => "TibannaGasStation",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown mining facility type.")
        };
    }
}
