using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.Stations
{
    public sealed class HostedProductionAccess : IProductionAccess
    {
        private readonly SelfProductionAccess _host;
        public PlayerId ProductionHost => _host.ProductionHost;
        public bool CanProduce => _host.CanProduce;

        public HostedProductionAccess(PlayerId host, IStationRegistry stations)
        {
            _host = new SelfProductionAccess(host, stations);
        }

        public bool TryGetProductionBase(out IEntity productionBase) =>
            _host.TryGetProductionBase(out productionBase);
    }
}
