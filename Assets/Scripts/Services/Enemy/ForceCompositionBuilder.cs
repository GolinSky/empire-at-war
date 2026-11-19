using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.EnemyFaction.Models.Intel;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Health;
using GameEntity = EmpireAtWar.Entities.BaseEntity.IEntity;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.DefendPlatform;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>Collects living ships and squadrons into a <see cref="ForceComposition"/> at their current health.</summary>
    public sealed class ForceCompositionBuilder
    {
        private readonly IEntityLocator _entityLocator;
        private readonly UnitCombatProfileCatalog _catalog;
        private readonly AiWorldSnapshot _worldSnapshot;

        public ForceCompositionBuilder(
            IEntityLocator entityLocator,
            UnitCombatProfileCatalog catalog,
            AiWorldSnapshot worldSnapshot)
        {
            _entityLocator = entityLocator;
            _catalog = catalog;
            _worldSnapshot = worldSnapshot;
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

            IReadOnlyList<AiWorldSnapshot.Entry> units = _worldSnapshot.LivingUnits;
            for (int i = 0; i < units.Count; i++)
            {
                AiWorldSnapshot.Entry entry = units[i];
                if (entry.Entity.HealthModel.IsDestroyed || !include(entry.Entity))
                {
                    continue;
                }

                force.Add(_catalog.Get(entry.UnitTypeId), entry.Hull, entry.Shields, entry.OffenseScale);
                if (unitCounts != null)
                {
                    unitCounts.TryGetValue(entry.UnitTypeId, out int count);
                    unitCounts[entry.UnitTypeId] = count + 1;
                }
            }
        }

        public void AddUnit(ForceComposition force, GameEntity entity)
        {
            IHealthModelObserver health = entity.HealthModel;
            force.Add(_catalog.Get(entity.GetFacade<IUnitTypeFacade>().UnitTypeId),
                health.Hull, health.Shields, GetOffenseScale(health));
        }

        /// <summary>Adds a satellite or defend platform at its current health, rated by its platform type.</summary>
        public void AddStructure(ForceComposition force, GameEntity structure)
        {
            IHealthModelObserver health = structure.HealthModel;
            force.Add(_catalog.GetPlatform(structure.GetFacade<IDefendPlatformTypeFacade>().Type),
                health.Hull, health.Shields, GetOffenseScale(health));
        }

        /// <summary>Counts live carrier wings and usable empty slots; records deployed ship counts for pending purchases.</summary>
        public int CountHangarSquadrons(PlayerId owner, Dictionary<UnitTypeId, int> liveShips)
        {
            liveShips.Clear();
            int count = 0;
            IReadOnlyList<AiWorldSnapshot.Entry> owned = _worldSnapshot.GetOwnedUnits(owner);
            for (int i = 0; i < owned.Count; i++)
            {
                AiWorldSnapshot.Entry entry = owned[i];
                if (!entry.UnitTypeId.IsShip || entry.Entity.HealthModel.IsDestroyed) continue;
                liveShips.TryGetValue(entry.UnitTypeId, out int ships);
                liveShips[entry.UnitTypeId] = ships + 1;
                count += entry.HangarSquadrons;
            }
            return count;
        }

        /// <summary>
        /// Clears <paramref name="force"/> and fills it with the hostile units the team still remembers, at the strength
        /// they were last seen with. This is how the AI sees hostiles: through intel, not the map. Age makes a sighting's
        /// position uncertain, not the unit weaker: discounting unseen units made the AI attack fleets it had lost sight of.
        /// Destroyed units leave the intel at once, and forgotten sightings drop out here.
        /// </summary>
        public void BuildKnown(
            ForceComposition force,
            HostileIntelModel intel,
            float now,
            Predicate<HostileSighting> include)
        {
            force.Clear();
            foreach (HostileSighting sighting in intel.Sightings)
            {
                float confidence = intel.GetConfidence(sighting, now);
                if (confidence <= 0f || !include(sighting))
                {
                    continue;
                }

                force.Add(_catalog.Get(sighting.UnitTypeId), sighting.Hull, sighting.Shields, sighting.OffenseScale);
            }
        }

        /// <summary>Destroyed hardpoints (ship systems, squadron fighters) stop firing.</summary>
        public static float GetOffenseScale(IHealthModelObserver health)
        {
            if (health.HardPointModels.Count == 0)
            {
                return 1f;
            }

            int alive = 0;
            foreach (IReadOnlyHardPointModel hardPoint in health.HardPointModels)
            {
                if (!hardPoint.IsDestroyed)
                {
                    alive++;
                }
            }

            return (float)alive / health.HardPointModels.Count;
        }
    }
}
