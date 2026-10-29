using EmpireAtWar.Entities.BaseEntity;

namespace EmpireAtWar.Components.Ship.Health.HardPointOverlay
{
    public interface IHardPointOverlayLayout
    {
        bool TryGetMarkerTop(IEntity ship, out float screenTop);
    }
}
