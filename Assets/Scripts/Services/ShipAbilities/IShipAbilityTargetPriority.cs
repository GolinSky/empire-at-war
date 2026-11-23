using EmpireAtWar.Entities.BaseEntity;

namespace EmpireAtWar.Services.ShipAbilities
{
    /// <summary>
    /// Automatic target order for a targeted ability: the highest priority in range wins over the attack order,
    /// the ordered target breaks ties, then the nearest. Enemy stations become automatic candidates.
    /// </summary>
    public interface IShipAbilityTargetPriority
    {
        int GetTargetPriority(IEntity target);
    }
}
