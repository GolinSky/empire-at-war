using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.Repair
{
    /// <summary>
    /// Host-only repair rules for every <see cref="IRepairSourceFacade"/>: each repairer ticks on its own interval,
    /// keeps its targets until they are full or leave its range, and fills free slots with the most damaged allied
    /// ships and structures that fewer than <see cref="RepairProfile.MaxHealersPerTarget"/> repairers already serve.
    /// </summary>
    public sealed class RepairService : ITickable, IInitializable, ILateDisposable
    {
        private const float STAGGER_STEP = 0.37f;

        private sealed class Repairer
        {
            public IEntity Entity;
            public IRepairSourceFacade Source;
            public float Timer;
            public readonly List<IEntity> Targets = new List<IEntity>();
        }

        private readonly struct Candidate
        {
            public readonly IEntity Entity;
            public readonly float HullPercentage;

            public Candidate(IEntity entity, float hullPercentage)
            {
                Entity = entity;
                HullPercentage = hullPercentage;
            }
        }

        private sealed class MostDamagedFirst : IComparer<Candidate>
        {
            public int Compare(Candidate x, Candidate y) => x.HullPercentage.CompareTo(y.HullPercentage);
        }

        private static readonly MostDamagedFirst MOST_DAMAGED_FIRST = new MostDamagedFirst();

        private readonly IEntityLocator _entities;
        private readonly IPlayerRelations _relations;
        private readonly IMatchSession _session;
        private readonly List<Repairer> _repairers = new List<Repairer>();
        private readonly Dictionary<long, int> _claims = new Dictionary<long, int>();
        private readonly List<Candidate> _candidates = new List<Candidate>();
        private int _addedRepairers;

        public RepairService(IEntityLocator entities, IPlayerRelations relations, IMatchSession session)
        {
            _entities = entities;
            _relations = relations;
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
            Clear();
        }

        /// <summary>Number of repairers currently working on the entity.</summary>
        public int GetHealerCount(long entityId) => _claims.TryGetValue(entityId, out int count) ? count : 0;

        public void Tick() => Advance(Time.deltaTime);

        public void Advance(float deltaTime)
        {
            if (!_session.IsAuthority) return;
            for (int i = 0; i < _repairers.Count; i++)
            {
                Repairer repairer = _repairers[i];
                repairer.Timer -= deltaTime;
                if (repairer.Timer > 0f) continue;
                float interval = repairer.Source.Profile.Interval;
                // A long frame runs one tick, not a burst of catch-up repairs.
                repairer.Timer = Mathf.Max(repairer.Timer + interval, 0f);
                RunTick(repairer);
            }
        }

        public void Clear()
        {
            foreach (Repairer repairer in _repairers)
            {
                if (repairer.Targets.Count == 0) continue;
                repairer.Targets.Clear();
                repairer.Source.SetTargets(repairer.Targets);
            }
            _repairers.Clear();
            _claims.Clear();
            _candidates.Clear();
            _addedRepairers = 0;
        }

        private void RunTick(Repairer repairer)
        {
            IRepairSourceFacade source = repairer.Source;
            if (!source.CanRepair)
            {
                ReleaseAll(repairer);
                return;
            }

            RepairProfile profile = source.Profile;
            Vector3 origin = source.Position;
            float radiusSquared = profile.Radius * profile.Radius;
            bool changed = false;
            List<IEntity> targets = repairer.Targets;
            for (int i = targets.Count - 1; i >= 0; i--)
            {
                if (IsValidTarget(repairer, targets[i], origin, radiusSquared, out _)) continue;
                Release(targets[i].Id);
                targets.RemoveAt(i);
                changed = true;
            }

            if (targets.Count < profile.MaxTargets)
            {
                _candidates.Clear();
                foreach (IEntity entity in _entities.Entities)
                {
                    if (targets.Contains(entity) || GetHealerCount(entity.Id) >= profile.MaxHealersPerTarget ||
                        !IsValidTarget(repairer, entity, origin, radiusSquared, out IRepairableFacade repairable))
                        continue;
                    _candidates.Add(new Candidate(entity, repairable.HullPercentage));
                }
                _candidates.Sort(MOST_DAMAGED_FIRST);
                for (int i = 0; i < _candidates.Count && targets.Count < profile.MaxTargets; i++)
                {
                    IEntity entity = _candidates[i].Entity;
                    targets.Add(entity);
                    _claims[entity.Id] = GetHealerCount(entity.Id) + 1;
                    changed = true;
                }
                _candidates.Clear();
            }

            float fraction = profile.FractionPerTick;
            for (int i = 0; i < targets.Count; i++)
                targets[i].GetFacade<IRepairableFacade>().Repair(fraction);
            if (changed) source.SetTargets(targets);
        }

        private bool IsValidTarget(Repairer repairer, IEntity entity, Vector3 origin, float radiusSquared,
            out IRepairableFacade repairable)
        {
            repairable = null;
            return entity != repairer.Entity && !entity.HealthModel.IsDestroyed &&
                   _relations.IsAllied(repairer.Entity.Owner, entity.Owner) &&
                   entity.TryGetFacade(out repairable) && repairable.NeedsRepair &&
                   entity.TryGetFacade(out IEntityTransformFacade transform) &&
                   (transform.Transform.position - origin).sqrMagnitude <= radiusSquared;
        }

        private void HandleEntityAdded(IEntity entity)
        {
            if (!entity.TryGetFacade(out IRepairSourceFacade source)) return;
            // Staggered first ticks keep many repairers built together from all working on the same frame.
            float stagger = _addedRepairers++ * STAGGER_STEP % 1f;
            _repairers.Add(new Repairer { Entity = entity, Source = source, Timer = source.Profile.Interval * stagger });
        }

        private void HandleEntityRemoved(IEntity entity)
        {
            for (int i = _repairers.Count - 1; i >= 0; i--)
            {
                Repairer repairer = _repairers[i];
                if (repairer.Entity == entity)
                {
                    ReleaseAll(repairer);
                    _repairers.RemoveAt(i);
                    continue;
                }
                if (!repairer.Targets.Remove(entity)) continue;
                Release(entity.Id);
                repairer.Source.SetTargets(repairer.Targets);
            }
            _claims.Remove(entity.Id);
        }

        private void ReleaseAll(Repairer repairer)
        {
            if (repairer.Targets.Count == 0) return;
            foreach (IEntity target in repairer.Targets) Release(target.Id);
            repairer.Targets.Clear();
            repairer.Source.SetTargets(repairer.Targets);
        }

        private void Release(long entityId)
        {
            int count = GetHealerCount(entityId) - 1;
            if (count > 0) _claims[entityId] = count;
            else _claims.Remove(entityId);
        }
    }
}
