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
        public PlayerId ProductionHost { get; }
        public bool CanProduce => TryGetProductionBase(out _);
        public bool HasHostBase => _stations.HasProductionBase(ProductionHost);

        public SelfProductionAccess(PlayerId owner, IStationRegistry stations, IPlayerRoster players)
        {
            ProductionHost = owner;
            _stations = stations;
            _players = players;
        }

        public bool TryGetProductionBase(out IEntity productionBase)
        {
            if (_stations.TryGetProductionBase(ProductionHost, out productionBase)) return true;

            foreach (PlayerSlot player in _players.Players)
            {
                if (player.Id != ProductionHost && _players.IsAllied(player.Id, ProductionHost) &&
                    _stations.TryGetProductionBase(player.Id, out productionBase))
                    return true;
            }

            productionBase = null;
            return false;
        }
    }
}
