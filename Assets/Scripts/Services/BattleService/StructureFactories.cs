using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.SpaceStation;

namespace EmpireAtWar.Services.Battle
{
    /// <summary>One player's structure factories; asteroid facilities and platforms use their own prefabs.</summary>
    public sealed class StructureFactories
    {
        public SpaceStationFactory Stations { get; }
        public MiningFacilityFactory MiningFacilities { get; }
        public AsteroidMiningFacilityFactory AsteroidMiningFacilities { get; }
        public DefendPlatformFactory DefendPlatforms { get; }
        public AsteroidDefendPlatformFactory AsteroidDefendPlatforms { get; }

        public StructureFactories(SpaceStationFactory stations, MiningFacilityFactory miningFacilities,
            AsteroidMiningFacilityFactory asteroidMiningFacilities, DefendPlatformFactory defendPlatforms,
            AsteroidDefendPlatformFactory asteroidDefendPlatforms)
        {
            Stations = stations;
            MiningFacilities = miningFacilities;
            AsteroidMiningFacilities = asteroidMiningFacilities;
            DefendPlatforms = defendPlatforms;
            AsteroidDefendPlatforms = asteroidDefendPlatforms;
        }
    }
}
