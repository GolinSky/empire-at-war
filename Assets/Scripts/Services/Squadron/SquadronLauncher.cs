using EmpireAtWar.Models.Players;
using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Services.Stations;

namespace EmpireAtWar.Services.Squadrons
{
    /// <summary>Launches bought squadrons from the hangar of the owner's space station.</summary>
    public sealed class SquadronLauncher : ISquadronLauncher
    {
        private readonly IStationRegistry _stationRegistry;

        public SquadronLauncher(IStationRegistry stationRegistry)
        {
            _stationRegistry = stationRegistry;
        }

        public bool TryLaunchFromStation(PlayerId owner, SquadronType squadronType, out ISquadron squadron)
        {
            if (_stationRegistry.TryGetLivingStation(owner, out IEntity station) &&
                station.TryGetFacade(out IHangarCommand hangar))
            {
                squadron = hangar.Launch(squadronType);
                return true;
            }

            squadron = null;
            return false;
        }
    }
}
