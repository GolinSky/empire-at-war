using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.Stations
{
    /// <summary>
    /// Produces at the host's own base; after it falls, at any living production base of the host's team.
    /// </summary>
    public sealed class SelfProductionAccess : IProductionAccess
    {
        private readonly IStationRegistry _stations;
        private readonly IPlayerRoster _players;
        private readonly bool _stationsOnly;
        public PlayerId ProductionHost { get; }
        public bool CanProduce => TryGetProductionBase(out _);
        public bool HasHostBase => _stations.HasProductionBase(ProductionHost, _stationsOnly);

        public SelfProductionAccess(PlayerId owner, IStationRegistry stations, IPlayerRoster players,
            bool stationsOnly = false)
        {
            ProductionHost = owner;
            _stations = stations;
            _players = players;
            _stationsOnly = stationsOnly;
        }

        public bool TryGetProductionBase(out IEntity productionBase)
        {
            if (_stations.TryGetProductionBase(ProductionHost, out productionBase, _stationsOnly)) return true;

            foreach (PlayerSlot player in _players.Players)
            {
                if (player.Id != ProductionHost && _players.IsAllied(player.Id, ProductionHost) &&
                    _stations.TryGetProductionBase(player.Id, out productionBase, _stationsOnly))
                    return true;
            }

            productionBase = null;
            return false;
        }
    }
}
