using System.Collections.Generic;
using EmpireAtWar.Entities.EnemyFaction.Controllers;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.CaptureSites;

namespace EmpireAtWar.Services.Player
{
    public sealed class PlayerRegistry : IPlayerRegistry
    {
        private readonly Dictionary<PlayerId, ISiteFacilityBuilder> _siteBuilders =
            new Dictionary<PlayerId, ISiteFacilityBuilder>();
        private readonly Dictionary<PlayerId, IEnemyReinforcementObserver> _aiReinforcements =
            new Dictionary<PlayerId, IEnemyReinforcementObserver>();
        private readonly Dictionary<PlayerId, IStationSpawner> _stationSpawners =
            new Dictionary<PlayerId, IStationSpawner>();

        public void RegisterSiteBuilder(PlayerId owner, ISiteFacilityBuilder builder)
        {
            _siteBuilders.Add(owner, builder);
        }

        public void RegisterAiReinforcement(PlayerId owner, IEnemyReinforcementObserver reinforcement)
        {
            _aiReinforcements.Add(owner, reinforcement);
        }

        public void RegisterStationSpawner(PlayerId owner, IStationSpawner spawner)
        {
            _stationSpawners.Add(owner, spawner);
        }

        public void UnregisterSiteBuilder(PlayerId owner)
        {
            _siteBuilders.Remove(owner);
        }

        public void UnregisterAiReinforcement(PlayerId owner)
        {
            _aiReinforcements.Remove(owner);
        }

        public void UnregisterStationSpawner(PlayerId owner)
        {
            _stationSpawners.Remove(owner);
        }

        public ISiteFacilityBuilder GetSiteBuilder(PlayerId owner)
        {
            return _siteBuilders[owner];
        }

        public IStationSpawner GetStationSpawner(PlayerId owner)
        {
            return _stationSpawners[owner];
        }

        public bool HasPendingReinforcement(PlayerId owner)
        {
            return _aiReinforcements.TryGetValue(owner, out IEnemyReinforcementObserver reinforcement) &&
                reinforcement.HasPendingReinforcement;
        }
    }
}
