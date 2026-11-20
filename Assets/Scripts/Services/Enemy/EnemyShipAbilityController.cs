using System.Collections.Generic;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Services.ShipAbilities;
using UnityEngine;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Utils;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>
    /// AI ship abilities. Auto-power roles and auto-use rules come from the same <see cref="AutoPowerDecider"/> the
    /// player's auto-power uses (round 5, E10); this controller only paces it by difficulty
    /// (<see cref="EnemyAiDifficultyProfile.AbilityDecisionInterval"/>, <see cref="EnemyAiDifficultyProfile.AbilityUseChance"/>),
    /// closes Lock S-Foils outside a retreat, and fires the few abilities without an auto-power role
    /// (<see cref="ShipAbilityAiUse.Escape"/> and <see cref="ShipAbilityAiUse.Offensive"/>).
    /// </summary>
    public sealed class EnemyShipAbilityController : ITickable
    {
        // Casters are worked through a few per frame, so one decision never lands in a single frame.
        private const int CASTERS_PER_TICK = 4;

        private readonly IEntityLocator _entities;
        private readonly IEnemyAiStateProvider _state;
        private readonly IPlayerRelations _relations;
        private readonly SimulationModel _simulation;

        private readonly PlayerSlot _owner;
        private readonly ShipAbilityService _shipAbilityService;
        private readonly AutoPowerDecider _decider;
        private readonly AutoPowerTuning _tuning;
        private readonly List<IEntity> _targets = new List<IEntity>();
        // The caster's entity is kept beside its facade: the facade resolves its entity through the locator,
        // which throws once the unit has left the battle.
        private readonly List<(IEntity Entity, IShipAbilityFacade Caster)> _casters =
            new List<(IEntity, IShipAbilityFacade)>();
        // Membership is snapshotted; lifetime and position are checked when each caster acts.
        private readonly List<IEntity> _hostiles = new List<IEntity>();
        private EnemyAiDifficultyProfile _profile;
        private int _nextCaster;

        private float _now;
        private float _timeLeft;
        private int _cyclesUntilUse;
        private bool _useThisCycle;
        private bool _hasDecided;

        public EnemyShipAbilityController(IEntityLocator entities,
            IEnemyAiStateProvider state, IPlayerRelations relations,
            ShipAbilityService shipAbilityService,
            PlayerSlot owner, ShipAbilityAutoUse autoUse, AutoPowerCombatSnapshot combat, ShipAbilityCatalog catalog,
            SimulationModel simulation = null)
        {
            _owner = owner;
            _entities = entities;
            _state = state;
            _relations = relations;
            _shipAbilityService = shipAbilityService;
            _simulation = simulation;
            _tuning = catalog.AutoPower;
            // The AI runs on the host: its abilities start directly instead of being requested.
            _decider = new AutoPowerDecider(shipAbilityService, combat, autoUse, _tuning, shipAbilityService.TryActivate);
        }

        public void Tick()
        {
            if (_simulation != null && !_simulation.Setup.UseAbilities) return;
            _now += Time.deltaTime;
            _timeLeft -= Time.deltaTime;
            if (_timeLeft <= 0f) StartDecision();
            UseNextCasters();
        }

        // Shield loss is sampled at the auto-power pace; the difficulty interval only paces what is used.
        private void StartDecision()
        {
            _profile = EnemyAiDifficultyProfile.Get(_owner.Difficulty);
            _timeLeft = _tuning.DecisionInterval;
            if (!_hasDecided)
            {
                _hasDecided = true;
                _timeLeft += EnemyAiStagger.Offset(_owner.Id, _profile.AbilityDecisionInterval);
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
                    _hostiles.Add(entity);
                }
            }
            _decider.Capture();
            // The difficulty paces use: one cycle in every AbilityDecisionInterval may start abilities.
            _useThisCycle = _cyclesUntilUse <= 0;
            _cyclesUntilUse = _useThisCycle
                ? Mathf.Max(1, Mathf.RoundToInt(_profile.AbilityDecisionInterval / _tuning.DecisionInterval)) - 1
                : _cyclesUntilUse - 1;
        }

        private void UseNextCasters()
        {
            int end = Mathf.Min(_nextCaster + CASTERS_PER_TICK, _casters.Count);
            for (; _nextCaster < end; _nextCaster++)
            {
                (IEntity entity, IShipAbilityFacade caster) = _casters[_nextCaster];
                if (!_entities.TryGetEntity(entity.Id, out IEntity current) ||
                    !ReferenceEquals(current, entity) || entity.HealthModel.IsDestroyed)
                {
                    _decider.Remove(entity.Id);
                    continue;
                }
                CloseSFoils(caster);
                entity.TryGetFacade(out IShipAutoPowerFacade automation);
                if (!_useThisCycle || Random.value >= _profile.AbilityUseChance)
                {
                    _decider.Observe(entity, caster, automation, _now);
                    continue;
                }
                if (_decider.Check(entity, caster, automation, _now)) continue;
                for (int i = 0; i < caster.Slots.Count; i++)
                {
                    ShipAbilitySlot slot = caster.Slots[i];
                    if (slot.CanActivate && slot.Definition.AutoPower == AutoPowerRole.None &&
                        !ShipAbilityAutoUse.HasRule(slot.Definition))
                        TryUseUnrated(caster, slot);
                }
            }
        }

        private void CloseSFoils(IShipAbilityFacade caster)
        {
            for (int i = 0; i < caster.Slots.Count; i++)
            {
                ShipAbilitySlot slot = caster.Slots[i];
                if (slot.Id == ShipAbilityId.LockSFoils && slot.State == ShipAbilityState.Active &&
                    _state.CurrentState != EnemyStrategicState.RetreatValue)
                    _shipAbilityService.TryCancel(caster, slot.Id);
            }
        }

        /// <summary>Abilities without an auto-power role or auto-use rule: escapes while retreating, attacks on
        /// visible hostiles.</summary>
        private void TryUseUnrated(IShipAbilityFacade caster, ShipAbilitySlot slot)
        {
            ShipAbilityDefinition definition = slot.Definition;
            if (definition.AiUse == ShipAbilityAiUse.Escape)
            {
                if (_state.CurrentState == EnemyStrategicState.RetreatValue)
                    _shipAbilityService.TryActivate(caster, slot.Id, null);
                return;
            }
            if (definition.AiUse != ShipAbilityAiUse.Offensive) return;
            if (!definition.RequiresEnemyTarget)
            {
                CollectTargets(caster, definition, inAbilityRange: false);
                if (_targets.Count > 0) _shipAbilityService.TryActivate(caster, slot.Id, null);
                return;
            }
            CollectTargets(caster, definition, inAbilityRange: true);
            // Target eligibility differs by ability; a rejected preferred target must not block the rest.
            while (_targets.Count > 0)
            {
                IEntity target = Random.value < _profile.AbilityTargetPrecision
                    ? BestTarget() : _targets[Random.Range(0, _targets.Count)];
                if (_shipAbilityService.TryActivate(caster, slot.Id, target)) return;
                _targets.Remove(target);
            }
        }

        private void CollectTargets(IShipAbilityFacade caster, ShipAbilityDefinition definition, bool inAbilityRange)
        {
            _targets.Clear();
            foreach (IEntity entity in _hostiles)
            {
                if (!_entities.TryGetEntity(entity.Id, out IEntity current) ||
                    !ReferenceEquals(current, entity)) continue;
                if (inAbilityRange)
                {
                    if (_shipAbilityService.CanTarget(caster, definition, entity) &&
                        ShipAbilityRange.IsInRange(caster, entity, definition.Range)) _targets.Add(entity);
                }
                else if (_shipAbilityService.IsVisibleHostile(_owner.Id, entity) &&
                         PlanarGeometry.Distance(caster.WorldPosition,
                             entity.GetFacade<IEntityTransformFacade>().Transform.position) <= caster.RadarRange)
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
