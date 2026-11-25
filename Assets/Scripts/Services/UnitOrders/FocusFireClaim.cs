using EmpireAtWar.Entities.BaseEntity;

namespace EmpireAtWar.Services.UnitOrders
{
    public sealed class FocusFireClaim : IFocusFireClaim
    {
        private readonly IFocusFireSlots _slots;
        private readonly IEntityLocator _locator;
        private readonly long _entityId;

        public FocusFireClaim(IFocusFireSlots slots, IEntityLocator locator, long entityId)
        {
            _slots = slots;
            _locator = locator;
            _entityId = entityId;
        }

        private IEntity Self => _locator.GetEntity(_entityId);

        public bool HasSlot(IEntity target) => _slots.HasSlotFor(Self, target);

        public bool TryClaim(IEntity target) => _slots.TryClaim(Self, target);

        public void Release() => _slots.Release(_entityId);
    }
}
