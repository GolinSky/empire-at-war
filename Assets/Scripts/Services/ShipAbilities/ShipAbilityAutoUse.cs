using EmpireAtWar.Entities.BaseEntity.EntityFacades;

namespace EmpireAtWar.Services.ShipAbilities
{
    /// <summary>Evaluates <see cref="IShipAbilityAutoUseRule"/> settings, so callers need no concrete ability types.</summary>
    public sealed class ShipAbilityAutoUse
    {
        private readonly ShipAbilityDependencies _dependencies;

        public ShipAbilityAutoUse(ShipAbilityDependencies dependencies)
        {
            _dependencies = dependencies;
        }

        public static bool HasRule(ShipAbilityDefinition definition) => definition.Settings is IShipAbilityAutoUseRule;

        public bool ShouldActivate(IShipAbilityFacade caster, ShipAbilityDefinition definition) =>
            definition.Settings is IShipAbilityAutoUseRule rule && rule.ShouldAutoActivate(_dependencies, caster);
    }
}
