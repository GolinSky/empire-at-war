using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    public sealed class FighterReinforcementsAbility : IShipAbility
    {
        public void Start(IShipAbilityFacade caster, ShipAbilityDefinition definition, IEntity target) =>
            caster.Entity.GetFacade<IHangarCommand>().TryLaunchReplacement();
    }
}
