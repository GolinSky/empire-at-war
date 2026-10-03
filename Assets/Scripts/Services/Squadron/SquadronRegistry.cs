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
    public sealed class SquadronRegistry : Service, ISquadronRegistry, IDisposable
    {
        private readonly IEntityLocator _entityLocator;

        private readonly List<IEntity> _squadrons = new List<IEntity>();

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
        }

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
            }
        }

        private void HandleEntityRemoved(IEntity entity)
        {
            _squadrons.Remove(entity);
        }
    }
}
