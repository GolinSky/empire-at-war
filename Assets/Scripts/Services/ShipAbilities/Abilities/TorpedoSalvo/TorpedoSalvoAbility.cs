using EmpireAtWar.Components.Combat;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    public sealed class TorpedoSalvoAbility : IShipAbility
    {
        private readonly TorpedoSalvoSettings _settings;
        private CombatModifiers _modifiers;

        public TorpedoSalvoAbility(TorpedoSalvoSettings settings) { _settings = settings; }

        public void Start(IShipAbilityFacade caster, ShipAbilityDefinition definition, IEntity target)
        {
            _modifiers = caster.Modifiers;
            _modifiers.SetTorpedoSalvo(_settings.FireDelayMultiplier);
        }

        public void Stop() => _modifiers.SetTorpedoSalvo(1f);
    }
}
