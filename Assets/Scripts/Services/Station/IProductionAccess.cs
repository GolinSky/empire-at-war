using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.Stations
{
    public interface IProductionAccess
    {
        bool CanProduce { get; }
        PlayerId ProductionHost { get; }
        bool TryGetProductionBase(out IEntity productionBase);
    }
}
