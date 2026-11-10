using EmpireAtWar.Entities.BaseEntity.EntityFacades;

namespace EmpireAtWar.Services.ShipAbilities
{
    /// <summary>Settings that decide on their own when the ability should fire without a target, for AI and auto-power.</summary>
    public interface IShipAbilityAutoUseRule
    {
        bool ShouldAutoActivate(ShipAbilityDependencies dependencies, IShipAbilityFacade caster);
    }
}
