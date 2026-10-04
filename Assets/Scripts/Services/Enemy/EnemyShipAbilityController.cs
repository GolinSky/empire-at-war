using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Services.ShipAbilities;
using UnityEngine;
using Zenject;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Utils;

namespace EmpireAtWar.Services.Enemy
{
    public sealed class EnemyShipAbilityController : ITickable
    {
        private readonly IEntityLocator _entities;
        private readonly IEnemyAiStateProvider _state;
        private readonly IPlayerRelations _relations;

        private readonly PlayerSlot _owner;
        private readonly ShipAbilityService _shipAbilityService;
        private readonly List<IEntity> _targets = new List<IEntity>();

        private float _timeLeft;

        public EnemyShipAbilityController(IEntityLocator entities,
            IEnemyAiStateProvider state, IPlayerRelations relations,
            ShipAbilityService shipAbilityService,
            PlayerSlot owner)
        {
            _owner = owner;
            _entities = entities;
            _state = state;
            _relations = relations;
            _shipAbilityService = shipAbilityService;
        }

        public void Tick()
        {
            _timeLeft -= Time.deltaTime;
            if (_timeLeft > 0f) return;
            EnemyAiDifficultyProfile profile = EnemyAiDifficultyProfile.Get(_owner.Difficulty);
            _timeLeft = profile.AbilityDecisionInterval;
            foreach (IEntity entity in _entities.Entities)
            {
                if (entity.Owner != _owner.Id || entity.HealthModel.IsDestroyed ||
                    !entity.TryGetFacade(out IShipAbilityFacade caster)) continue;
                for (int i = 0; i < caster.Slots.Count; i++)
                {
                    ShipAbilitySlot slot = caster.Slots[i];
                    if (slot.State != ShipAbilityState.Ready ||
                        Random.value >= profile.AbilityUseChance) continue;
                    TryUse(caster, slot, profile);
                }
            }
        }

        private void TryUse(IShipAbilityFacade caster, ShipAbilitySlot slot,
            EnemyAiDifficultyProfile profile)
        {
            ShipAbilityDefinition definition = slot.Definition;
            float searchRange = definition.AiUse == ShipAbilityAiUse.Defensive
                ? caster.RadarRange :
                definition.RequiresEnemyTarget ? definition.Range : caster.RadarRange;
            CollectTargets(caster, searchRange);
            if (definition.AiUse == ShipAbilityAiUse.Defensive &&
                (caster.Health.ShieldPercentage >= profile.RetreatShieldThreshold ||
                 _targets.Count == 0)) return;
            if (definition.AiUse == ShipAbilityAiUse.Escape &&
                _state.CurrentState != EnemyStrategicState.RetreatValue) return;
            if (definition.AiUse == ShipAbilityAiUse.Offensive && _targets.Count == 0) return;

            IEntity target = null;
            if (definition.RequiresEnemyTarget)
            {
                CollectTargets(caster, definition.Range);
                if (_targets.Count == 0) return;
                target = Random.value < profile.AbilityTargetPrecision
                    ? BestTarget() : _targets[Random.Range(0, _targets.Count)];
            }
            _shipAbilityService.TryActivate(caster, slot.Id, target);
        }

        private void CollectTargets(IShipAbilityFacade caster, float range)
        {
            _targets.Clear();
            foreach (IEntity entity in _entities.Entities)
            {
                if (!_relations.IsHostile(caster.Entity.Owner, entity.Owner) || entity.HealthModel.IsDestroyed ||
                    PlanarGeometry.Distance(caster.WorldPosition, entity.GetFacade<IEntityTransformFacade>().Transform.position) > range)
                    continue;
                _targets.Add(entity);
            }
        }

        private IEntity BestTarget()
        {
            IEntity best = _targets[0];
            for (int i = 1; i < _targets.Count; i++)
            {
                IEntity candidate = _targets[i];
                bool candidateShip = candidate.IsShip();
                bool bestShip = best.IsShip();
                if (candidateShip && !bestShip ||
                    candidateShip == bestShip &&
                    candidate.HealthModel.HullPercentage < best.HealthModel.HullPercentage)
                    best = candidate;
            }
            return best;
        }
    }
}
