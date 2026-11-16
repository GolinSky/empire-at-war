using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Squadrons.EntityFacades;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    public sealed class ReplenishWingmenAbility : IShipAbility
    {
        public void Start(IShipAbilityFacade caster, ShipAbilityDefinition definition, IEntity target) =>
            caster.Entity.GetFacade<IReplenishWingmenFacade>().ReplenishWingmen();
    }
}
