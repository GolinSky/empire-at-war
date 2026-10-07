using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Health;
using GameEntity = EmpireAtWar.Entities.BaseEntity.IEntity;
using EmpireAtWar.Entities.BaseEntity;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>Collects living ships and squadrons into a <see cref="ForceComposition"/> at their current health.</summary>
    public sealed class ForceCompositionBuilder
    {
        private readonly IEntityLocator _entityLocator;
        private readonly UnitCombatProfileCatalog _catalog;

        public ForceCompositionBuilder(IEntityLocator entityLocator, UnitCombatProfileCatalog catalog)
        {
            _entityLocator = entityLocator;
            _catalog = catalog;
        }

        /// <summary>Clears <paramref name="force"/> and fills it with every living unit <paramref name="include"/> accepts.</summary>
        /// <param name="unitCounts">Optional; receives how many living units of each type were added.</param>
        public void Build(
            ForceComposition force,
            Predicate<GameEntity> include,
            Dictionary<UnitTypeId, int> unitCounts = null)
        {
            force.Clear();
            if (unitCounts != null)
            {
                unitCounts.Clear();
            }

            foreach (GameEntity entity in _entityLocator.Entities)
            {
                IHealthModelObserver health = entity.HealthModel;
                if (health.IsDestroyed ||
                    !health.HasUnits ||
                    !entity.TryGetFacade(out IUnitTypeFacade unit) ||
                    !include(entity))
                {
                    continue;
                }

                force.Add(_catalog.Get(unit.UnitTypeId), health.Hull, health.Shields, GetOffenseScale(health));
                if (unitCounts != null)
                {
                    unitCounts.TryGetValue(unit.UnitTypeId, out int count);
                    unitCounts[unit.UnitTypeId] = count + 1;
                }
            }
        }

        /// <summary>Destroyed hardpoints (ship systems, squadron fighters) stop firing.</summary>
        private static float GetOffenseScale(IHealthModelObserver health)
        {
            if (health.HardPointModels.Length == 0)
            {
                return 1f;
            }

            int alive = 0;
            foreach (HardPointModel hardPoint in health.HardPointModels)
            {
                if (!hardPoint.IsDestroyed)
                {
                    alive++;
                }
            }

            return (float)alive / health.HardPointModels.Length;
        }
    }
}
