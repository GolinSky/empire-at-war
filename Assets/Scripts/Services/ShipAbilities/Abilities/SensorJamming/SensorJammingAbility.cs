using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Utils;

namespace EmpireAtWar.Services.ShipAbilities.Abilities
{
    public sealed class SensorJammingAbility : IPhasedShipAbility
    {
        private readonly IEntityLocator _entities;
        private readonly IPlayerRelations _relations;
        private readonly HashSet<CombatModifiers> _affected = new HashSet<CombatModifiers>();
        private readonly HashSet<CombatModifiers> _nearby = new HashSet<CombatModifiers>();
        private IShipAbilityFacade _caster;
        private float _range;

        public SensorJammingAbility(IEntityLocator entities, IPlayerRelations relations)
        {
            _entities = entities;
            _relations = relations;
        }

        public bool IsComplete => !HasSensor(_caster);
        public bool SurvivesCasterDeath => false;
        public bool CanStart(IShipAbilityFacade caster, IEntity target) => HasSensor(caster);

        public void Start(IShipAbilityFacade caster, ShipAbilityDefinition definition, IEntity target)
        {
            _caster = caster;
            _range = definition.Range;
            Advance(0);
        }

        public void Advance(float deltaTime)
        {
            _nearby.Clear();
            foreach (IEntity entity in _entities.Entities)
            {
                if (entity.HealthModel.IsDestroyed || !_relations.IsHostile(_caster.Entity.Owner, entity.Owner) ||
                    !entity.TryGetFacade(out ICombatModifiersFacade enemy) ||
                    !entity.TryGetFacade(out IEntityTransformFacade transform) ||
                    PlanarGeometry.Distance(_caster.WorldPosition, transform.Transform.position) > _range)
                    continue;
                _nearby.Add(enemy.Modifiers);
                if (_affected.Add(enemy.Modifiers)) enemy.Modifiers.AddSensorJamming();
            }
            _affected.RemoveWhere(RemoveDistant);
        }

        public void Stop()
        {
            foreach (CombatModifiers modifiers in _affected) modifiers.RemoveSensorJamming();
            _affected.Clear();
            _nearby.Clear();
        }

        private static bool HasSensor(IShipAbilityFacade caster) =>
            caster.Health.GetShipUnits(HardPointType.SensorArray)
                .Any(point => point.HardPointType == HardPointType.SensorArray && !point.IsDestroyed);

        private bool RemoveDistant(CombatModifiers modifiers)
        {
            if (_nearby.Contains(modifiers)) return false;
            modifiers.RemoveSensorJamming();
            return true;
        }
    }
}
