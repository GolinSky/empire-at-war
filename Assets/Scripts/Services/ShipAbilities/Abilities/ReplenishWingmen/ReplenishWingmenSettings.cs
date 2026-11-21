using System;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Squadrons.EntityFacades;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    /// <summary>Automatic use: as soon as an escort is missing.</summary>
    [Serializable]
    public sealed class ReplenishWingmenSettings : ShipAbilitySettings, IShipAbilityReadiness, IShipAbilityAutoUseRule
    {
        public bool IsReady(IShipAbilityFacade caster) =>
            caster.Entity.GetFacade<IReplenishWingmenFacade>().CanReplenishWingmen;

        public bool ShouldAutoActivate(ShipAbilityDependencies dependencies, IShipAbilityFacade caster) =>
            IsReady(caster);

        public override IShipAbility CreateAbility(ShipAbilityDependencies dependencies) =>
            new ReplenishWingmenAbility();
    }
}
