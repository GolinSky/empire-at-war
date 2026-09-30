using EmpireAtWar.Entities.BaseEntity.Orders;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    public interface IUnitOrderObserverFacade : IEntityFacade { UnitOrderType CurrentOrder { get; } }
}
