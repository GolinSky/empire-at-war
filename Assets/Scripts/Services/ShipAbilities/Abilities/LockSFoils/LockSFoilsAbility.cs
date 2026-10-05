using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Squadrons.Flight;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Entities.Squadrons.EntityFacades;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    public sealed class LockSFoilsAbility : IShipAbility
    {
        private readonly LockSFoilsSettings _settings;
        private CombatModifiers _modifiers;
        private SFoilsModel _model;

        public LockSFoilsAbility(LockSFoilsSettings settings) { _settings = settings; }

        public void Start(IShipAbilityFacade caster, ShipAbilityDefinition definition, IEntity target)
        {
            _modifiers = caster.Modifiers;
            _model = caster.Entity.GetFacade<ISFoilsFacade>().Model;
            _modifiers.Add(_settings.StatModifier);
            _model.SetClosed(true);
        }

        public void Stop()
        {
            _modifiers.Remove(_settings.StatModifier);
            _model.SetClosed(false);
        }
    }
}
