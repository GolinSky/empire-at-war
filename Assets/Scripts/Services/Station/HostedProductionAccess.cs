using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.Stations
{
    public sealed class HostedProductionAccess : IProductionAccess
    {
        private readonly SelfProductionAccess _host;
        public PlayerId ProductionHost => _host.ProductionHost;
        public bool CanProduce => _host.CanProduce;
        public bool HasHostBase => _host.HasHostBase;

        public HostedProductionAccess(PlayerId host, IStationRegistry stations, IPlayerRoster players)
        {
            _host = new SelfProductionAccess(host, stations, players);
        }

        public bool TryGetProductionBase(out IEntity productionBase) =>
            _host.TryGetProductionBase(out productionBase);
    }
}
