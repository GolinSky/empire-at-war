using EmpireAtWar.Entities.BaseEntity;

namespace EmpireAtWar.Services.Battle
{
    public interface IHoveredEntityObserver
    {
        bool CanHover { get; }
        bool TryGetHovered(out IEntity entity);
        bool TryGetHardPoint(out IEntity entity, out int hardPointId);
    }
}
