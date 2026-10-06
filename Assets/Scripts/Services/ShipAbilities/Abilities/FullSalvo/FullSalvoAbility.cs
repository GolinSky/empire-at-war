using EmpireAtWar.Components.Combat;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship.Abilities;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    public sealed class FullSalvoAbility : IShipAbility
    {
        private readonly FullSalvoSettings _settings;
        private CombatModifiers _modifiers;

        public FullSalvoAbility(FullSalvoSettings settings) { _settings = settings; }

        public void Start(IShipAbilityFacade caster, ShipAbilityDefinition definition, IEntity target)
        {
            _modifiers = caster.Modifiers;
            _modifiers.SetFullSalvo(_settings.ProjectileFireDelayMultiplier, _settings.OtherFireDelayMultiplier);
        }

        public void Stop() => _modifiers.SetFullSalvo(1f, 1f);
    }
}
