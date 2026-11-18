using EmpireAtWar.Lifecycle;
using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Mvc;
using UnityEngine;
using EmpireAtWar.Entities.Units;

namespace EmpireAtWar.Services.Squadrons
{
    /// <summary>Tracks squadrons registered in the <see cref="IEntityLocator"/>.</summary>
    public sealed class SquadronRegistry : Service, ISquadronRegistry, IOrderedDisposable
    {
        private readonly IEntityLocator _entityLocator;

        private readonly List<IEntity> _squadrons = new List<IEntity>();
        private readonly Dictionary<PlayerId, int> _livingCounts = new Dictionary<PlayerId, int>();
        private bool _countsDirty = true;

        public SquadronRegistry(IEntityLocator entityLocator)
        {
            _entityLocator = entityLocator;
            foreach (IEntity entity in _entityLocator.Entities)
            {
                HandleEntityAdded(entity);
            }

            _entityLocator.EntityAdded += HandleEntityAdded;
            _entityLocator.EntityRemoved += HandleEntityRemoved;
        }

        public void Dispose()
        {
            _entityLocator.EntityAdded -= HandleEntityAdded;
            _entityLocator.EntityRemoved -= HandleEntityRemoved;
            foreach (IEntity squadron in _squadrons) squadron.HealthModel.OnDestroy -= InvalidateCounts;
        }

        public int GetLivingCount(PlayerId owner)
        {
            if (_countsDirty)
            {
                _livingCounts.Clear();
                foreach (IEntity squadron in _squadrons)
                {
                    if (squadron.HealthModel.IsDestroyed) continue;
                    _livingCounts.TryGetValue(squadron.Owner, out int count);
                    _livingCounts[squadron.Owner] = count + 1;
                }
                _countsDirty = false;
            }
            return _livingCounts.TryGetValue(owner, out int living) ? living : 0;
        }

        public int GetLivingCraftCount(PlayerId owner)
        {
            int count = 0;
            foreach (IEntity squadron in _squadrons)
            {
                if (squadron.Owner != owner || squadron.HealthModel.IsDestroyed) continue;
                var members = squadron.HealthModel.HardPointModels;
                for (int index = 0; index < members.Count; index++)
                    if (!members[index].IsDestroyed) count++;
            }
            return count;
        }

        private void InvalidateCounts() => _countsDirty = true;

        public void AddSquadronStrength(Func<Vector3, bool> contains, float weight, CaptureStrengthBuilder tally)
        {
            foreach (IEntity squadron in _squadrons)
            {
                if (IsLivingInside(squadron, contains))
                {
                    tally.Add(squadron.Owner, weight);
                }
            }
        }

        public bool HasSquadronInside(Func<Vector3, bool> contains, Predicate<PlayerId> isOwnerIncluded)
        {
            foreach (IEntity squadron in _squadrons)
            {
                if (isOwnerIncluded(squadron.Owner) && IsLivingInside(squadron, contains))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsLivingInside(IEntity squadron, Func<Vector3, bool> contains)
        {
            // A squadron is positioned at the centroid of its fighters.
            return !squadron.HealthModel.IsDestroyed &&
                contains(squadron.GetFacade<IEntityTransformFacade>().Transform.position);
        }

        private void HandleEntityAdded(IEntity entity)
        {
            if (entity.IsSquadron())
            {
                _squadrons.Add(entity);
                entity.HealthModel.OnDestroy += InvalidateCounts;
                InvalidateCounts();
            }
        }

        private void HandleEntityRemoved(IEntity entity)
        {
            if (!_squadrons.Remove(entity)) return;
            entity.HealthModel.OnDestroy -= InvalidateCounts;
            InvalidateCounts();
        }
    }
}
