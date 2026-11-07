using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using System;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class RapidDeploymentSettings : ShipAbilitySettings, IShipAbilityReadiness
    {
        public bool IsReady(IShipAbilityFacade caster) =>
            caster.Entity.GetFacade<IHangarCommand>().CanLaunchImmediately;

        public override IShipAbility CreateAbility(ShipAbilityDependencies dependencies) =>
            new RapidDeploymentAbility();
    }
}
