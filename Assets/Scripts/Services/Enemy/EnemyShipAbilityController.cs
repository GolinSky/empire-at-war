using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Ship;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.Enemy
{
    public sealed class EnemyShipAbilityController : ITickable
    {
        private readonly IEntityLocator _entities;
        private readonly IEnemyAiStateProvider _state;
        private readonly IGameModelObserver _game;
        private readonly ShipAbilityService _abilities;
        private readonly List<IEntity> _targets = new List<IEntity>();
        private float _timeLeft;

        public EnemyShipAbilityController(IEntityLocator entities,
            IEnemyAiStateProvider state, IGameModelObserver game,
            ShipAbilityService abilities)
        {
            _entities = entities;
            _state = state;
            _game = game;
            _abilities = abilities;
        }

        public void Tick()
        {
            _timeLeft -= Time.deltaTime;
            if (_timeLeft > 0f) return;
            EnemyAiDifficultyProfile profile = EnemyAiDifficultyProfile.Get(_game.EnemyDifficulty);
            _timeLeft = profile.AbilityDecisionInterval;
            foreach (IEntity entity in _entities.Entities)
            {
                if (entity.PlayerType != PlayerType.Opponent || entity.HealthModel.IsDestroyed ||
                    !entity.TryGetCommand(out IShipAbilityCommand caster)) continue;
                for (int i = 0; i < caster.Slots.Count; i++)
                {
                    ShipAbilitySlot slot = caster.Slots[i];
                    if (slot.State != ShipAbilityState.Ready ||
                        Random.value >= profile.AbilityUseChance) continue;
                    TryUse(caster, slot, profile);
                }
            }
        }

        private void TryUse(IShipAbilityCommand caster, ShipAbilitySlot slot,
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
            _abilities.TryActivate(caster, slot.Id, target);
        }

        private void CollectTargets(IShipAbilityCommand caster, float range)
        {
            _targets.Clear();
            foreach (IEntity entity in _entities.Entities)
            {
                if (entity.PlayerType == caster.Entity.PlayerType || entity.HealthModel.IsDestroyed ||
                    Vector3.Distance(caster.WorldPosition, entity.HealthModel.Transform.position) > range)
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
                bool candidateShip = candidate.Model is IShipModelObserver;
                bool bestShip = best.Model is IShipModelObserver;
                if (candidateShip && !bestShip ||
                    candidateShip == bestShip &&
                    candidate.HealthModel.ArmorPercentage < best.HealthModel.ArmorPercentage)
                    best = candidate;
            }
            return best;
        }
    }
}
