using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.Stations
{
    public sealed class SelfProductionAccess : IProductionAccess
    {
        private readonly IStationRegistry _stations;
        public PlayerId ProductionHost { get; }
        public bool CanProduce => _stations.HasProductionBase(ProductionHost);

        public SelfProductionAccess(PlayerId owner, IStationRegistry stations)
        {
            ProductionHost = owner;
            _stations = stations;
        }

        public bool TryGetProductionBase(out IEntity productionBase) =>
            _stations.TryGetProductionBase(ProductionHost, out productionBase);
    }
}
