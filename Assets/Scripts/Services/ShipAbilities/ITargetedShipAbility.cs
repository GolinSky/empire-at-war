using EmpireAtWar.Entities.BaseEntity;

namespace EmpireAtWar.Services.ShipAbilities
{
    /// <summary>A running ability aimed at one enemy entity.</summary>
    public interface ITargetedShipAbility
    {
        IEntity Target { get; }
    }
}
