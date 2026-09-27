using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.Orders;

namespace EmpireAtWar.Components.Ship.Health.HardPointOverlay
{
    /// <summary>
    /// Presentation state of the hardpoint overlay: the inspected ship, the hardpoint under the cursor
    /// and the hardpoint the player explicitly ordered an attack on.
    /// </summary>
    public sealed class HardPointOverlayModel : IHardPointHoverObserver
    {
        private bool _isHoveredTargetable;

        public IEntity InspectedShip { get; private set; }
        public int HoveredHardPointId { get; private set; } = UnitOrderModel.NO_HARD_POINT;
        public IEntity TargetedShip { get; private set; }
        public int TargetedHardPointId { get; private set; } = UnitOrderModel.NO_HARD_POINT;

        public void Inspect(IEntity ship, int hoveredHardPointId, bool isHoveredTargetable)
        {
            InspectedShip = ship;
            HoveredHardPointId = ship == null ? UnitOrderModel.NO_HARD_POINT : hoveredHardPointId;
            _isHoveredTargetable = isHoveredTargetable;
        }

        public void SetTarget(IEntity ship, int hardPointId)
        {
            TargetedShip = ship;
            TargetedHardPointId = hardPointId;
        }

        public void ClearTarget() => SetTarget(null, UnitOrderModel.NO_HARD_POINT);

        public bool IsTargeted(IEntity ship, int hardPointId) =>
            TargetedShip != null && TargetedShip.Id == ship.Id && TargetedHardPointId == hardPointId;

        public bool TryGetHovered(out IEntity ship, out int hardPointId)
        {
            ship = InspectedShip;
            hardPointId = HoveredHardPointId;
            return ship != null && hardPointId != UnitOrderModel.NO_HARD_POINT && _isHoveredTargetable;
        }
    }
}
