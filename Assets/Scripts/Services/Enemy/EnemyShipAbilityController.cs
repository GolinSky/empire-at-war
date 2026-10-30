using System.Collections.Generic;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.Vision;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Utils;

namespace EmpireAtWar.Services.Enemy
{
    public sealed class EnemyShipAbilityController : ITickable
    {
        // Casters are worked through a few per frame, so one decision never lands in a single frame.
        private const int CASTERS_PER_TICK = 4;

        private readonly IEntityLocator _entities;
        private readonly IEnemyAiStateProvider _state;
        private readonly IPlayerRelations _relations;
        private readonly IVisionService _vision;
        private readonly SimulationModel _simulation;

        private readonly PlayerSlot _owner;
        private readonly ShipAbilityService _shipAbilityService;
        private readonly List<IEntity> _targets = new List<IEntity>();
        // The caster's entity is kept beside its facade: the facade resolves its entity through the locator,
        // which throws once the unit has left the battle.
        private readonly List<(IEntity Entity, IShipAbilityFacade Caster)> _casters =
            new List<(IEntity, IShipAbilityFacade)>();
        // Hostile units and where they were when the decision started; every caster searches this one list.
        private readonly List<(IEntity Entity, Vector3 Position)> _hostiles = new List<(IEntity, Vector3)>();
        private EnemyAiDifficultyProfile _profile;
        private int _nextCaster;

        private float _timeLeft;
        private bool _hasDecided;

        public EnemyShipAbilityController(IEntityLocator entities,
            IEnemyAiStateProvider state, IPlayerRelations relations,
            ShipAbilityService shipAbilityService, IVisionService vision,
            PlayerSlot owner, SimulationModel simulation = null)
        {
            _owner = owner;
            _entities = entities;
            _state = state;
            _relations = relations;
            _shipAbilityService = shipAbilityService;
            _vision = vision;
            _simulation = simulation;
        }

        public void Tick()
        {
            if (_simulation != null && !_simulation.Setup.UseAbilities) return;
            _timeLeft -= Time.deltaTime;
            if (_timeLeft <= 0f) StartDecision();
            UseNextCasters();
        }

        private void StartDecision()
        {
            _profile = EnemyAiDifficultyProfile.Get(_owner.Difficulty);
            _timeLeft = _profile.AbilityDecisionInterval;
            if (!_hasDecided)
            {
                _hasDecided = true;
                _timeLeft += EnemyAiStagger.Offset(_owner.Id, _timeLeft);
            }

            // Abilities can bring units into the battle (fighter reinforcements), which changes the entity
            // collection, so casters and hostiles are collected before any ability is used.
            _casters.Clear();
            _hostiles.Clear();
            _nextCaster = 0;
            foreach (IEntity entity in _entities.Entities)
            {
                if (entity.HealthModel.IsDestroyed) continue;
                if (entity.Owner == _owner.Id)
                {
                    if (entity.TryGetFacade(out IShipAbilityFacade caster)) _casters.Add((entity, caster));
                }
                else if (_relations.IsHostile(_owner.Id, entity.Owner))
                {
                    _hostiles.Add((entity, entity.GetFacade<IEntityTransformFacade>().Transform.position));
                }
            }
        }

        private void UseNextCasters()
        {
            int end = Mathf.Min(_nextCaster + CASTERS_PER_TICK, _casters.Count);
            for (; _nextCaster < end; _nextCaster++)
            {
                (IEntity entity, IShipAbilityFacade caster) = _casters[_nextCaster];
                if (!_entities.TryGetEntity(entity.Id, out _) || entity.HealthModel.IsDestroyed) continue;
                for (int i = 0; i < caster.Slots.Count; i++)
                {
                    ShipAbilitySlot slot = caster.Slots[i];
                    if (slot.Id == ShipAbilityId.LockSFoils &&
                        slot.State == ShipAbilityState.Active &&
                        _state.CurrentState != EnemyStrategicState.RetreatValue)
                    {
                        _shipAbilityService.TryCancel(caster, slot.Id);
                        continue;
                    }
                    if (!slot.CanActivate ||
                        Random.value >= _profile.AbilityUseChance) continue;
                    TryUse(caster, slot, _profile);
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

            if (definition.RequiresEnemyTarget)
            {
                CollectTargets(caster, definition.Range);
                // Target eligibility differs by ability; a rejected preferred target must not block the rest.
                while (_targets.Count > 0)
                {
                    IEntity target = Random.value < profile.AbilityTargetPrecision
                        ? BestTarget() : _targets[Random.Range(0, _targets.Count)];
                    if (_shipAbilityService.TryActivate(caster, slot.Id, target)) return;
                    _targets.Remove(target);
                }
                return;
            }
            _shipAbilityService.TryActivate(caster, slot.Id, null);
        }

        private void CollectTargets(IShipAbilityFacade caster, float range)
        {
            _targets.Clear();
            foreach ((IEntity entity, Vector3 position) in _hostiles)
            {
                if (entity.HealthModel.IsDestroyed || entity.IsCloaked() ||
                    !_vision.IsVisible(_owner.Id, entity.GetFacade<IEntityTransformFacade>().Transform.position) ||
                    PlanarGeometry.Distance(caster.WorldPosition, position) > range)
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
