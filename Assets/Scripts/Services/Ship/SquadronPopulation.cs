using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Ship
{
    public static class SquadronPopulation
    {
        /// <summary>Replaces the destination contents with every squadron that is not destroyed.</summary>
        public static void CollectLivingSquadrons(this IEntityLocator entityLocator, List<IEntity> destination)
        {
            destination.Clear();
            foreach (IEntity entity in entityLocator.Entities)
            {
                if (entity.Model is ISquadronModelObserver && !entity.HealthModel.IsDestroyed)
                {
                    destination.Add(entity);
                }
            }
        }

        /// <summary>Adds every squadron inside the area to the capture tally with the given weight.</summary>
        public static void AddSquadronStrength(this IReadOnlyList<IEntity> squadrons, Func<Vector3, bool> contains,
            float weight, CaptureTallyBuilder tally)
        {
            // A squadron is positioned at the centroid of its fighters.
            for (int i = 0; i < squadrons.Count; i++)
            {
                IEntity squadron = squadrons[i];
                if (contains(squadron.GetFacade<IEntityTransformFacade>().Transform.position))
                {
                    tally.Add(squadron.Owner, weight);
                }
            }
        }
    }
}
