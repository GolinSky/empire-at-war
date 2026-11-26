using EmpireAtWar.Entities.BaseEntity;

namespace EmpireAtWar.Services.ShipAbilities
{
    /// <summary>
    /// Automatic target order for a targeted ability without an attack order: the highest priority in range wins,
    /// then the nearest. Enemy stations become automatic candidates. A live attack-order target always overrides it.
    /// </summary>
    public interface IShipAbilityTargetPriority
    {
        int GetTargetPriority(IEntity target);
    }
}
