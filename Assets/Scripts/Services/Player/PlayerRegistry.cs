using System.Collections.Generic;
using EmpireAtWar.Controllers.Economy;
using EmpireAtWar.Entities.EnemyFaction.Controllers;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.Squadrons;

namespace EmpireAtWar.Services.Player
{
    public sealed class PlayerRegistry : IPlayerRegistry
    {
        private readonly Dictionary<PlayerId, ReinforcementModel> _reinforcementReserves =
            new Dictionary<PlayerId, ReinforcementModel>();
        private readonly Dictionary<PlayerId, ISiteFacilityBuilder> _siteBuilders =
            new Dictionary<PlayerId, ISiteFacilityBuilder>();
        private readonly Dictionary<PlayerId, IEnemyReinforcementObserver> _aiReinforcements =
            new Dictionary<PlayerId, IEnemyReinforcementObserver>();
        private readonly Dictionary<PlayerId, IEnemyStationCheat> _aiStationCheats =
            new Dictionary<PlayerId, IEnemyStationCheat>();
        private readonly Dictionary<PlayerId, IStationSpawner> _stationSpawners =
            new Dictionary<PlayerId, IStationSpawner>();
        private readonly Dictionary<PlayerId, ISquadronPrewarmer> _squadronPrewarmers =
            new Dictionary<PlayerId, ISquadronPrewarmer>();
        private readonly Dictionary<PlayerId, IEconomyProvider> _economies =
            new Dictionary<PlayerId, IEconomyProvider>();

        public void RegisterSiteBuilder(PlayerId owner, ISiteFacilityBuilder builder)
        {
            _siteBuilders.Add(owner, builder);
        }

        public void RegisterAiReinforcement(PlayerId owner, IEnemyReinforcementObserver reinforcement)
        {
            _aiReinforcements.Add(owner, reinforcement);
        }

        public void RegisterAiStationCheat(PlayerId owner, IEnemyStationCheat station)
        {
            _aiStationCheats.Add(owner, station);
        }

        public void RegisterStationSpawner(PlayerId owner, IStationSpawner spawner)
        {
            _stationSpawners.Add(owner, spawner);
        }

        public void RegisterSquadronPrewarmer(PlayerId owner, ISquadronPrewarmer prewarmer)
        {
            _squadronPrewarmers.Add(owner, prewarmer);
        }

        public void RegisterEconomy(PlayerId owner, IEconomyProvider economy)
        {
            _economies.Add(owner, economy);
        }

        public void UnregisterSiteBuilder(PlayerId owner)
        {
            _siteBuilders.Remove(owner);
        }

        public void UnregisterAiReinforcement(PlayerId owner)
        {
            _aiReinforcements.Remove(owner);
        }

        public void UnregisterAiStationCheat(PlayerId owner)
        {
            _aiStationCheats.Remove(owner);
        }

        public void UnregisterStationSpawner(PlayerId owner)
        {
            _stationSpawners.Remove(owner);
        }

        public void UnregisterSquadronPrewarmer(PlayerId owner)
        {
            _squadronPrewarmers.Remove(owner);
        }

        public void UnregisterEconomy(PlayerId owner)
        {
            _economies.Remove(owner);
        }

        public ISiteFacilityBuilder GetSiteBuilder(PlayerId owner)
        {
            return _siteBuilders[owner];
        }

        public IStationSpawner GetStationSpawner(PlayerId owner)
        {
            return _stationSpawners[owner];
        }

        public ISquadronPrewarmer GetSquadronPrewarmer(PlayerId owner)
        {
            return _squadronPrewarmers[owner];
        }

        public IEconomyProvider GetEconomy(PlayerId owner)
        {
            return _economies[owner];
        }

        public IEnemyStationCheat GetAiStationCheat(PlayerId owner)
        {
            return _aiStationCheats[owner];
        }

        public bool HasPendingReinforcement(PlayerId owner)
        {
            return (_aiReinforcements.TryGetValue(owner, out IEnemyReinforcementObserver reinforcement) &&
                reinforcement.HasPendingReinforcement) ||
                (_reinforcementReserves.TryGetValue(owner, out ReinforcementModel reserve) && reserve.HasReserve);
        }

        public void RegisterReinforcementReserve(PlayerId owner, ReinforcementModel reserve) =>
            _reinforcementReserves.Add(owner, reserve);

        public void UnregisterReinforcementReserve(PlayerId owner) => _reinforcementReserves.Remove(owner);
    }
}
