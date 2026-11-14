using EmpireAtWar.Components.Combat;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    public sealed class BoostWeaponPowerAbility : IStoppableShipAbility, IReplicaPersistentShipAbility
    {
        private readonly BoostWeaponPowerSettings _settings;
        private CombatModifiers _modifiers;
        private CombatModifierHandle _applied;

        public BoostWeaponPowerAbility(BoostWeaponPowerSettings settings) { _settings = settings; }

        public void Start(IShipAbilityFacade caster, ShipAbilityDefinition definition, IEntity target)
        {
            _modifiers = caster.Modifiers;
            _applied = _modifiers.Add(_settings.StatModifier);
            _modifiers.SetWeaponPowerShotInterval(_settings.ShotIntervalMultiplier);
        }

        public void Stop()
        {
            _modifiers.Remove(_applied);
            _modifiers.SetWeaponPowerShotInterval(1f);
        }
    }
}
