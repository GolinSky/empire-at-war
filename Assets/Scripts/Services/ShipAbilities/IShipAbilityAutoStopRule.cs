using EmpireAtWar.Entities.BaseEntity.EntityFacades;

namespace EmpireAtWar.Services.ShipAbilities
{
    /// <summary>Settings of a lasting ability that decide on their own when it should end, for AI and auto-power.</summary>
    public interface IShipAbilityAutoStopRule
    {
        bool ShouldAutoStop(ShipAbilityDependencies dependencies, IShipAbilityFacade caster);
    }
}
