using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using System;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    [Serializable]
    public sealed class FighterReinforcementsSettings : ShipAbilitySettings, IShipAbilityReadiness
    {
        public bool IsReady(IShipAbilityFacade caster) =>
            caster.Entity.GetFacade<IHangarCommand>().CanLaunchReplacement;

        public override IShipAbility CreateAbility(ShipAbilityDependencies dependencies) =>
            new FighterReinforcementsAbility();
    }
}
