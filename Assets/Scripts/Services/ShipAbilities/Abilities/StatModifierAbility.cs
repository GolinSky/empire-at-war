using EmpireAtWar.Components.Combat;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    /// <summary>Applies one stat modifier to the caster while active.</summary>
    public sealed class StatModifierAbility : IShipAbility
    {
        private readonly CombatStatModifier _modifier;
        private CombatModifiers _modifiers;
        private CombatModifierHandle _applied;

        public StatModifierAbility(CombatStatModifier modifier) { _modifier = modifier; }

        public void Start(IShipAbilityFacade caster, ShipAbilityDefinition definition, IEntity target)
        {
            _modifiers = caster.Modifiers;
            _applied = _modifiers.Add(_modifier);
        }

        public void Stop() => _modifiers.Remove(_applied);
    }
}
