using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    public sealed class FighterReinforcementsAbility : IPhasedShipAbility
    {
        public bool IsComplete => true;
        public bool SurvivesCasterDeath => false;

        public bool CanStart(IShipAbilityFacade caster, IEntity target) =>
            caster.Entity.GetFacade<IHangarCommand>().CanLaunchReplacement;

        public void Start(IShipAbilityFacade caster, ShipAbilityDefinition definition, IEntity target) =>
            caster.Entity.GetFacade<IHangarCommand>().TryLaunchReplacement();

        public void Advance(float deltaTime) { }
        public void Stop() { }
    }
}
