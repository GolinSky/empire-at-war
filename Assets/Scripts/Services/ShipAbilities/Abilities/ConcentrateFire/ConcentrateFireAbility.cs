using System.Collections.Generic;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Ship.Abilities;
using UnityEngine;
using EmpireAtWar.Utils;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    public sealed class ConcentrateFireAbility : IShipAbility
    {
        private readonly IEntityLocator _entities;

        private readonly ConcentrateFireSettings _settings;
        private readonly List<CombatModifiers> _affected = new List<CombatModifiers>();
        private CombatModifiers _focusModifiers;

        public ConcentrateFireAbility(IEntityLocator entities, ConcentrateFireSettings settings)
        {
            _settings = settings;
            _entities = entities;
        }

        public void Start(IShipAbilityFacade caster, ShipAbilityDefinition definition, IEntity target)
        {
            if (_settings.TargetDamageMultiplier > 1f)
            {
                _focusModifiers = caster.Modifiers;
                _focusModifiers.SetFocusFire(target, _settings.TargetDamageMultiplier);
                foreach (IEntity entity in _entities.Entities)
                {
                    if (entity.Owner != caster.Entity.Owner || entity.HealthModel.IsDestroyed ||
                        !entity.TryGetFacade(out IAttackFacade attack) ||
                        !entity.TryGetFacade(out IEntityTransformFacade transform) ||
                        PlanarGeometry.Distance(caster.WorldPosition, transform.Transform.position) > _settings.CommandRadius)
                        continue;
                    attack.Attack(target, Vector3.zero);
                }
                return;
            }

            foreach (IEntity entity in _entities.Entities)
            {
                if (entity.Owner != caster.Entity.Owner || entity.HealthModel.IsDestroyed ||
                    !entity.TryGetFacade(out IShipAbilityFacade ally) ||
                    !entity.TryGetFacade(out IAttackFacade attack) ||
                    PlanarGeometry.Distance(caster.WorldPosition, ally.WorldPosition) > _settings.CommandRadius)
                    continue;

                ally.Modifiers.Add(_settings.AllyStatModifier);
                _affected.Add(ally.Modifiers);
                attack.Attack(target, Vector3.zero);
            }
        }

        public void Stop()
        {
            if (_focusModifiers != null)
            {
                _focusModifiers.ClearFocusFire();
                _focusModifiers = null;
            }
            for (int i = 0; i < _affected.Count; i++) _affected[i].Remove(_settings.AllyStatModifier);
            _affected.Clear();
        }
    }
}
