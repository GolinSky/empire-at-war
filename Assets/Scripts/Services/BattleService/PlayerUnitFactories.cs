using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Services.Squadrons;
using EmpireAtWar.Ship;

namespace EmpireAtWar.Services.Battle
{
    /// <summary>One player's unit and structure factories.</summary>
    public sealed class PlayerUnitFactories
    {
        public ShipFactory Ships { get; }
        public ISquadronSpawner Squadrons { get; }
        public SpaceStationFactory Stations { get; }
        public MiningFacilityFactory MiningFacilities { get; }
        public DefendPlatformFactory DefendPlatforms { get; }

        public PlayerUnitFactories(ShipFactory ships, ISquadronSpawner squadrons, SpaceStationFactory stations,
            MiningFacilityFactory miningFacilities, DefendPlatformFactory defendPlatforms)
        {
            Ships = ships;
            Squadrons = squadrons;
            Stations = stations;
            MiningFacilities = miningFacilities;
            DefendPlatforms = defendPlatforms;
        }
    }
}
