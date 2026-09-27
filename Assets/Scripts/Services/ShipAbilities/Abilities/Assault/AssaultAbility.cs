using System;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Ship.Abilities;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    public sealed class AssaultAbility : IShipAbility
    {
        private readonly AssaultSettings _settings;
        private CombatModifiers _modifiers;

        public AssaultAbility(AssaultSettings settings) { _settings = settings; }

        public void Start(IShipAbilityFacade caster, ShipAbilityDefinition definition, IEntity target)
        {
            if (!caster.Entity.TryGetFacade(out IAttackFacade attack))
                throw new InvalidOperationException($"{nameof(AssaultAbility)} requires an {nameof(IAttackFacade)} on the caster.");

            _modifiers = caster.Modifiers;
            _modifiers.Add(_settings.StatModifier);
            attack.Attack(target, Vector3.zero);
        }

        public void Stop() => _modifiers.Remove(_settings.StatModifier);
    }
}
