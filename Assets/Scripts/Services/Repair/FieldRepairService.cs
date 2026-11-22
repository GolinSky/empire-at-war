using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Player;
using UnityEngine;

namespace EmpireAtWar.Services.Repair
{
    /// <summary>
    /// Host-only <see cref="ResearchType.FieldRepairs"/> rule: once a ship of a faction with the research has neither
    /// lost health nor had a target for <see cref="COMBAT_COOLDOWN_SECONDS"/>, it restores
    /// <see cref="PERCENT_PER_SECOND"/> of its hull and live hardpoints every second. It stacks with other repairs.
    /// </summary>
    public sealed class FieldRepairService : ITickable, IInitializable, ILateDisposable
    {
        public const float COMBAT_COOLDOWN_SECONDS = 10f;
        public const float PERCENT_PER_SECOND = 1f;
        private const float TICK_INTERVAL = 1f;
        private const float FRACTION_PER_TICK = PERCENT_PER_SECOND / 100f * TICK_INTERVAL;

        private sealed class Tracked
        {
            private readonly IEntity _entity;
            private float _lastHealth;

            public readonly IRepairableFacade Repairable;
            public readonly IUnitCombatObserverFacade Combat;
            public float CalmTime;

            public Tracked(IEntity entity, IRepairableFacade repairable, IUnitCombatObserverFacade combat)
            {
                _entity = entity;
                Repairable = repairable;
                Combat = combat;
                _lastHealth = Health;
            }

            private float Health => _entity.HealthModel.Hull + _entity.HealthModel.Shields;

            // Any loss of hull or shields counts as a hit; repairs and shield regeneration only raise them.
            public void HandleHealthChanged()
            {
                float health = Health;
                if (health < _lastHealth) CalmTime = 0f;
                _lastHealth = health;
            }
        }

        private readonly IEntityLocator _entities;
        private readonly IPlayerRegistry _players;
        private readonly IMatchSession _session;
        private readonly Dictionary<IEntity, Tracked> _tracked = new Dictionary<IEntity, Tracked>();
        private float _timer;

        public FieldRepairService(IEntityLocator entities, IPlayerRegistry players, IMatchSession session)
        {
            _entities = entities;
            _players = players;
            _session = session;
        }

        public void Initialize()
        {
            _entities.EntityAdded += HandleEntityAdded;
            _entities.EntityRemoved += HandleEntityRemoved;
            foreach (IEntity entity in _entities.Entities) HandleEntityAdded(entity);
        }

        public void LateDispose()
        {
            _entities.EntityAdded -= HandleEntityAdded;
            _entities.EntityRemoved -= HandleEntityRemoved;
            foreach (KeyValuePair<IEntity, Tracked> pair in _tracked)
                pair.Key.HealthModel.OnValueChanged -= pair.Value.HandleHealthChanged;
            _tracked.Clear();
        }

        public void Tick() => Advance(Time.deltaTime);

        public void Advance(float deltaTime)
        {
            if (!_session.IsAuthority) return;
            _timer -= deltaTime;
            if (_timer > 0f) return;
            // A long frame runs one tick, not a burst of catch-up repairs.
            _timer = Mathf.Max(_timer + TICK_INTERVAL, 0f);
            foreach (KeyValuePair<IEntity, Tracked> pair in _tracked) RunTick(pair.Key, pair.Value);
        }

        private void RunTick(IEntity entity, Tracked tracked)
        {
            if (tracked.Combat.Target != null)
            {
                tracked.CalmTime = 0f;
                return;
            }

            tracked.CalmTime += TICK_INTERVAL;
            if (tracked.CalmTime < COMBAT_COOLDOWN_SECONDS || entity.HealthModel.IsDestroyed ||
                !tracked.Repairable.NeedsRepair || !HasResearch(entity.Owner)) return;
            tracked.Repairable.Repair(FRACTION_PER_TICK);
        }

        private bool HasResearch(PlayerId owner) =>
            _players.TryGetResearch(owner, out IFactionResearchModelObserver research) &&
            research.GetCompletedTiers(ResearchType.FieldRepairs) > 0;

        // Ships only: they are the units with a combat observer.
        private void HandleEntityAdded(IEntity entity)
        {
            if (_tracked.ContainsKey(entity) ||
                !entity.TryGetFacade(out IUnitCombatObserverFacade combat) ||
                !entity.TryGetFacade(out IRepairableFacade repairable)) return;
            Tracked tracked = new Tracked(entity, repairable, combat);
            entity.HealthModel.OnValueChanged += tracked.HandleHealthChanged;
            _tracked.Add(entity, tracked);
        }

        private void HandleEntityRemoved(IEntity entity)
        {
            if (!_tracked.Remove(entity, out Tracked tracked)) return;
            entity.HealthModel.OnValueChanged -= tracked.HandleHealthChanged;
        }
    }
}
