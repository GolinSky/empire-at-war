using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Models.Players;

namespace EmpireAtWar.Services.Stations
{
    public interface IProductionAccess
    {
        /// <summary>True while any living production base of the host's team stands.</summary>
        bool CanProduce { get; }
        /// <summary>True while the host's own base stands; station upgrades and super weapons need it.</summary>
        bool HasHostBase { get; }
        PlayerId ProductionHost { get; }
        bool TryGetProductionBase(out IEntity productionBase);
    }
}
