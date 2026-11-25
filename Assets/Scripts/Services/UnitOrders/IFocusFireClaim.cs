using EmpireAtWar.Entities.BaseEntity;

namespace EmpireAtWar.Services.UnitOrders
{
    /// <summary>One unit's view of <see cref="IFocusFireSlots"/>: its own claim and whether a target has room for it.</summary>
    public interface IFocusFireClaim
    {
        bool HasSlot(IEntity target);

        /// <summary>Holds <paramref name="target"/> when a slot is free; otherwise the unit attacks it without a slot.</summary>
        bool TryClaim(IEntity target);

        void Release();
    }
}
