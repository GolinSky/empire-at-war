using System.Collections.Generic;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Ship.Abilities;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    public sealed class ConcentrateFireAbility : IShipAbility
    {
        private readonly ConcentrateFireSettings _settings;
        private readonly IEntityLocator _entities;
        private readonly List<CombatModifiers> _affected = new List<CombatModifiers>();

        public ConcentrateFireAbility(ConcentrateFireSettings settings, IEntityLocator entities)
        {
            _settings = settings;
            _entities = entities;
        }

        public void Start(IShipAbilityFacade caster, ShipAbilityDefinition definition, IEntity target)
        {
            foreach (IEntity entity in _entities.Entities)
            {
                if (entity.PlayerType != caster.Entity.PlayerType || entity.HealthModel.IsDestroyed ||
                    !entity.TryGetFacade(out IShipAbilityFacade ally) ||
                    !entity.TryGetFacade(out IAttackFacade attack) ||
                    Vector3.Distance(caster.WorldPosition, ally.WorldPosition) > _settings.CommandRadius)
                    continue;

                ally.Modifiers.Add(_settings.AllyStatModifier);
                _affected.Add(ally.Modifiers);
                attack.Attack(target, Vector3.zero);
            }
        }

        public void Stop()
        {
            for (int i = 0; i < _affected.Count; i++) _affected[i].Remove(_settings.AllyStatModifier);
            _affected.Clear();
        }
    }
}
