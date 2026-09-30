using System;
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

        public ISquadron LaunchFromStation(PlayerId owner, SquadronType squadronType)
        {
            if (_stationRegistry.TryGetLivingStation(owner, out IEntity station) &&
                station.TryGetFacade(out IHangarCommand hangar))
            {
                return hangar.Launch(squadronType);
            }

            throw new InvalidOperationException(
                $"{owner} has no operational space station with a hangar to launch {squadronType}.");
        }
    }
}
