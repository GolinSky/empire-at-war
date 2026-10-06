using EmpireAtWar.Components.Combat;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship.Abilities;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    public sealed class PowerToMainBatteriesAbility : IShipAbility
    {
        private readonly PowerToMainBatteriesSettings _settings;
        private CombatModifiers _modifiers;

        public PowerToMainBatteriesAbility(PowerToMainBatteriesSettings settings) { _settings = settings; }

        public void Start(IShipAbilityFacade caster, ShipAbilityDefinition definition, IEntity target)
        {
            _modifiers = caster.Modifiers;
            _modifiers.SetMainBatteries(true, _settings.FireDelayMultiplier);
            _modifiers.Add(_settings.StatModifier);
        }

        public void Stop()
        {
            _modifiers.SetMainBatteries(false, 1f);
            _modifiers.Remove(_settings.StatModifier);
        }
    }
}
