using System;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Squadrons.EntityFacades;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class ReplenishWingmenSettings : ShipAbilitySettings, IShipAbilityReadiness
    {
        public bool IsReady(IShipAbilityFacade caster) =>
            caster.Entity.GetFacade<IReplenishWingmenFacade>().CanReplenishWingmen;

        public override IShipAbility CreateAbility(ShipAbilityDependencies dependencies) =>
            new ReplenishWingmenAbility();
    }
}
