using System.Collections.Generic;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using UnityEngine;
using EmpireAtWar.Utils;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    public sealed class ConcentrateFireAbility : IStoppableShipAbility
    {
        private readonly IEntityLocator _entities;

        private readonly ConcentrateFireSettings _settings;
        private readonly List<(CombatModifiers Modifiers, CombatModifierHandle Applied)> _affected =
            new List<(CombatModifiers, CombatModifierHandle)>();
        private CombatModifiers _focusModifiers;

        public ConcentrateFireAbility(IEntityLocator entities, ConcentrateFireSettings settings)
        {
            _settings = settings;
            _entities = entities;
        }

        public void Start(IShipAbilityFacade caster, ShipAbilityDefinition definition, IEntity target) =>
            Start(caster, target, automatic: false, issueOrders: true);

        internal void Start(IShipAbilityFacade caster, IEntity target, bool automatic, bool issueOrders)
        {
            if (_settings.TargetDamageMultiplier > 1f)
            {
                _focusModifiers = caster.Modifiers;
                _focusModifiers.SetFocusFire(target, _settings.TargetDamageMultiplier);
                if (!issueOrders) return;
                foreach (IEntity entity in _entities.Entities)
                {
                    if (entity.Owner != caster.Entity.Owner || entity.HealthModel.IsDestroyed ||
                        !entity.TryGetFacade(out IAttackFacade attack) ||
                        !entity.TryGetFacade(out IEntityTransformFacade transform) ||
                        PlanarGeometry.Distance(caster.WorldPosition, transform.Transform.position) > _settings.CommandRadius)
                        continue;
                    if (!automatic || CanRedirect(entity)) attack.Attack(target, Vector3.zero);
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

                _affected.Add((ally.Modifiers, ally.Modifiers.Add(_settings.AllyStatModifier, _settings)));
                if (issueOrders && (!automatic || CanRedirect(entity))) attack.Attack(target, Vector3.zero);
            }
        }

        // Matching Attack orders already point at the target and retain their formation and hardpoint.
        private static bool CanRedirect(IEntity entity) =>
            entity.GetFacade<IUnitOrderObserverFacade>().CurrentOrder is
                UnitOrderType.None or UnitOrderType.AttackMove or UnitOrderType.Hunt;

        public void Stop()
        {
            if (_focusModifiers != null)
            {
                _focusModifiers.ClearFocusFire();
                _focusModifiers = null;
            }
            for (int i = 0; i < _affected.Count; i++) _affected[i].Modifiers.Remove(_affected[i].Applied);
            _affected.Clear();
        }
    }
}
