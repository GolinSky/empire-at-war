using EmpireAtWar.Entities.BaseEntity.Orders;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    public interface IUnitOrderObserverFacade : IEntityFacade
    {
        UnitOrderType CurrentOrder { get; }
        IEntity AttackTarget { get; }
        /// <summary>
        /// A ship on attack-move fights a warship that hit it on the way (W66); false for every other unit and order.
        /// </summary>
        bool IsEngagingEnRoute { get; }
    }
}
