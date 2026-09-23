using System.Collections.Generic;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using EmpireAtWar.Entities.Ship.Abilities;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    public sealed class ConcentrateFireAbility : IShipAbility
    {
        private readonly IEntityLocator _entities;
        private readonly List<CombatModifiers> _affected = new List<CombatModifiers>();
        private CombatStatModifier _modifier;

        public ConcentrateFireAbility(IEntityLocator entities) { _entities = entities; }

        public void Start(IShipAbilityCommand caster, ShipAbilityDefinition definition, IEntity target)
        {
            _modifier = definition.StatModifier;
            foreach (IEntity entity in _entities.Entities)
            {
                if (entity.PlayerType != caster.Entity.PlayerType || entity.HealthModel.IsDestroyed ||
                    !entity.TryGetCommand(out IShipAbilityCommand ally) ||
                    !entity.TryGetCommand(out IAttackCommand attack) ||
                    Vector3.Distance(caster.WorldPosition, ally.WorldPosition) > definition.CommandRadius)
                    continue;

                ally.Modifiers.Add(_modifier);
                _affected.Add(ally.Modifiers);
                attack.Attack(target, Vector3.zero);
            }
        }

        public void Stop()
        {
            for (int i = 0; i < _affected.Count; i++) _affected[i].Remove(_modifier);
            _affected.Clear();
        }
    }
}
