using EmpireAtWar.Components.Combat;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using EmpireAtWar.Entities.Ship.Abilities;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    public sealed class AssaultAbility : IShipAbility
    {
        private CombatModifiers _modifiers;
        private CombatStatModifier _modifier;

        public void Start(IShipAbilityCommand caster, ShipAbilityDefinition definition, IEntity target)
        {
            _modifiers = caster.Modifiers;
            _modifier = definition.StatModifier;
            _modifiers.Add(_modifier);
            caster.Entity.TryGetCommand(out IAttackCommand attack);
            attack.Attack(target, Vector3.zero);
        }

        public void Stop() => _modifiers.Remove(_modifier);
    }
}
