using EmpireAtWar.Components.Combat;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship.Abilities;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    public sealed class CloakAbility : IShipAbility
    {
        private CombatModifiers _modifiers;

        public void Start(IShipAbilityFacade caster, ShipAbilityDefinition definition, IEntity target)
        {
            _modifiers = caster.Modifiers;
            _modifiers.SetCloaked(true);
        }

        public void Stop() => _modifiers.SetCloaked(false);
    }
}
