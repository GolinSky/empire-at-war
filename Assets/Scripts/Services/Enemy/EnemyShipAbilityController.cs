using System.Collections.Generic;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Services.ShipAbilities;
using UnityEngine;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>
    /// AI ship abilities: every AI caster runs the same <see cref="AutoPowerDecider"/> the player's auto-power uses,
    /// auto-power roles and auto-use rules alike, at the auto-power pace. The AI has no ability rules of its own and
    /// difficulty does not change ability use.
    /// </summary>
    public sealed class EnemyShipAbilityController : ITickable
    {
        // Casters are worked through a few per frame, so one decision never lands in a single frame.
        private const int CASTERS_PER_TICK = 4;

        private readonly IEntityLocator _entities;
        private readonly SimulationModel _simulation;

        private readonly PlayerSlot _owner;
        private readonly AutoPowerDecider _decider;
        private readonly AutoPowerTuning _tuning;
        // The caster's entity is kept beside its facade: the facade resolves its entity through the locator,
        // which throws once the unit has left the battle.
        private readonly List<(IEntity Entity, IShipAbilityFacade Caster)> _casters =
            new List<(IEntity, IShipAbilityFacade)>();
        private int _nextCaster;

        private float _now;
        private float _timeLeft;
        private bool _hasDecided;

        public EnemyShipAbilityController(IEntityLocator entities, ShipAbilityService shipAbilityService,
            PlayerSlot owner, ShipAbilityAutoUse autoUse, AutoPowerCombatSnapshot combat, ShipAbilityCatalog catalog,
            SimulationModel simulation = null)
        {
            _owner = owner;
            _entities = entities;
            _simulation = simulation;
            _tuning = catalog.AutoPower;
            // The AI runs on the host: its abilities start and end directly instead of being requested.
            _decider = new AutoPowerDecider(shipAbilityService, combat, autoUse, _tuning, shipAbilityService.TryActivate,
                shipAbilityService.TryOwnerCancel);
        }

        public void Tick()
        {
            if (_simulation != null && !_simulation.Setup.UseAbilities) return;
            _now += Time.deltaTime;
            _timeLeft -= Time.deltaTime;
            if (_timeLeft <= 0f) StartDecision();
            UseNextCasters();
        }

        private void StartDecision()
        {
            _timeLeft = _tuning.DecisionInterval;
            if (!_hasDecided)
            {
                _hasDecided = true;
                _timeLeft += EnemyAiStagger.Offset(_owner.Id, _tuning.DecisionInterval);
            }

            // Abilities can bring units into the battle (fighter reinforcements), which changes the entity
            // collection, so casters are collected before any ability is used.
            _casters.Clear();
            _nextCaster = 0;
            foreach (IEntity entity in _entities.Entities)
                if (!entity.HealthModel.IsDestroyed && entity.Owner == _owner.Id &&
                    entity.TryGetFacade(out IShipAbilityFacade caster))
                    _casters.Add((entity, caster));
            _decider.Capture();
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
                entity.TryGetFacade(out IShipAutoPowerFacade automation);
                _decider.Check(entity, caster, automation, _now);
            }
        }
    }
}
