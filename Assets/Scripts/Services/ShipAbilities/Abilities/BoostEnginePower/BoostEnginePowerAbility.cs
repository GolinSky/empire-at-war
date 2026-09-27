using EmpireAtWar.Components.Combat;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship.Abilities;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    public sealed class BoostEnginePowerAbility : IShipAbility
    {
        private readonly BoostEnginePowerSettings _settings;
        private CombatModifiers _modifiers;

        public BoostEnginePowerAbility(BoostEnginePowerSettings settings) { _settings = settings; }

        public void Start(IShipAbilityFacade caster, ShipAbilityDefinition definition, IEntity target)
        {
            _modifiers = caster.Modifiers;
            _modifiers.Add(_settings.StatModifier);
        }

        public void Stop() => _modifiers.Remove(_settings.StatModifier);
    }
}
