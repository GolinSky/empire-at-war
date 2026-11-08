using EmpireAtWar.Entities.BaseEntity.Orders;

namespace EmpireAtWar.Entities.BaseEntity.EntityFacades
{
    /// <summary>
    /// Online: the host's current order of a unit, shown on clients, which do not run most orders themselves.
    /// <see cref="OrderRevision"/> changes with every new order, even one of the same type. Not used by gameplay.
    /// </summary>
    public interface IUnitOrderReplicaFacade : IEntityFacade
    {
        UnitOrderType CurrentOrder { get; }
        int OrderRevision { get; }

        /// <summary>Client: the host's order; <paramref name="isNewOrder"/> when it was just given (voice lines).</summary>
        void ApplyReplicaOrder(UnitOrderType order, bool isNewOrder);
    }
}
