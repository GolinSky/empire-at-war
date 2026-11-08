using EmpireAtWar.Entities.BaseEntity.Orders;
using Mirror;

namespace EmpireAtWar.Services.Networking.Messages
{
    /// <summary>Host → clients, reliable: a unit's current order changed; <see cref="IsNewOrder"/> for a fresh order.</summary>
    public struct UnitOrderStateMessage : NetworkMessage
    {
        public long EntityId;
        public UnitOrderType Order;
        public bool IsNewOrder;
    }
}
