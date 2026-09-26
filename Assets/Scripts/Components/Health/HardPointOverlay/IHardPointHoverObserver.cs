using EmpireAtWar.Entities.BaseEntity;

namespace EmpireAtWar.Components.Ship.Health.HardPointOverlay
{
    public interface IHardPointHoverObserver
    {
        /// <summary>The attackable hardpoint currently under the cursor, if any.</summary>
        bool TryGetHovered(out IEntity ship, out int hardPointId);
    }
}
