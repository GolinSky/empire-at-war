using System;
using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Hangar;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.EnemyFaction.Models.Intel;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Health;
using GameEntity = EmpireAtWar.Entities.BaseEntity.IEntity;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.DefendPlatform;
using UnityEngine;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>Collects living ships and squadrons into a <see cref="ForceComposition"/> at their current health.</summary>
    public sealed class ForceCompositionBuilder
    {
        private readonly IEntityLocator _entityLocator;
        private readonly UnitCombatProfileCatalog _catalog;
        private readonly AiWorldSnapshot _worldSnapshot;
        // A station's profile per entity, rebuilt when its count of firing weapons changes (level up, lost hardpoint).
        private readonly Dictionary<long, (int Weapons, UnitCombatProfile Profile)> _stationProfiles =
            new Dictionary<long, (int Weapons, UnitCombatProfile Profile)>();
        private readonly Dictionary<WeaponType, int> _stationWeapons = new Dictionary<WeaponType, int>();
        private readonly List<WeaponLoadoutEntry> _stationLoadout = new List<WeaponLoadoutEntry>();

        public ForceCompositionBuilder(
            IEntityLocator entityLocator,
            UnitCombatProfileCatalog catalog,
            AiWorldSnapshot worldSnapshot)
        {
            _entityLocator = entityLocator;
            _catalog = catalog;
            _worldSnapshot = worldSnapshot;
        }

        /// <summary>
        /// Clears <paramref name="force"/> and fills it with every living unit <paramref name="include"/> accepts.
        /// Returns their summed price.
        /// </summary>
        /// <param name="unitCounts">Optional; receives how many living units of each type were added.</param>
        public float Build(
            ForceComposition force,
            Predicate<GameEntity> include,
            Dictionary<UnitTypeId, int> unitCounts = null)
        {
            force.Clear();
            if (unitCounts != null)
            {
                unitCounts.Clear();
            }

            float value = 0f;
            IReadOnlyList<AiWorldSnapshot.Entry> units = _worldSnapshot.LivingUnits;
            for (int i = 0; i < units.Count; i++)
            {
                AiWorldSnapshot.Entry entry = units[i];
                if (entry.Entity.HealthModel.IsDestroyed || !include(entry.Entity))
                {
                    continue;
                }

                UnitCombatProfile profile = _catalog.Get(entry.UnitTypeId);
                force.Add(profile, entry.Hull, entry.Shields, entry.OffenseScale,
                    GetShieldRegen(profile, entry.Entity.HealthModel));
                value += profile.Price;
                if (unitCounts != null)
                {
                    unitCounts.TryGetValue(entry.UnitTypeId, out int count);
                    unitCounts[entry.UnitTypeId] = count + 1;
                }
            }
            return value;
        }

        /// <summary>
        /// Adds every living unit <paramref name="include"/> accepts that can be within <paramref name="areaRadius"/>
        /// of <paramref name="area"/> in <paramref name="seconds"/> at its cruise speed (W63). Ground plane.
        /// </summary>
        public void AddReachable(ForceComposition force, Predicate<GameEntity> include, Vector3 area, float areaRadius,
            float seconds)
        {
            IReadOnlyList<AiWorldSnapshot.Entry> units = _worldSnapshot.LivingUnits;
            for (int i = 0; i < units.Count; i++)
            {
                AiWorldSnapshot.Entry entry = units[i];
                if (entry.Entity.HealthModel.IsDestroyed || !include(entry.Entity)) continue;
                UnitCombatProfile profile = _catalog.Get(entry.UnitTypeId);
                float x = entry.Position.x - area.x;
                float z = entry.Position.z - area.z;
                if (!EnemyFleetRating.CanReach(Mathf.Sqrt(x * x + z * z), areaRadius, profile.Speed, seconds)) continue;
                force.Add(profile, entry.Hull, entry.Shields, entry.OffenseScale,
                    GetShieldRegen(profile, entry.Entity.HealthModel));
            }
        }

        /// <summary>The rated profile of a unit type: damage, durability, speed and price.</summary>
        public UnitCombatProfile GetProfile(UnitTypeId unitTypeId) => _catalog.Get(unitTypeId);

        /// <summary>
        /// Adds a station at its current health, rated by the weapons its installed, standing hardpoints fire (W62).
        /// A station never moves: it counts in fights at it, never in fleet strength.
        /// </summary>
        public void AddStation(ForceComposition force, GameEntity station)
        {
            IHealthModelObserver health = station.HealthModel;
            force.Add(GetStationProfile(station), health.Hull, health.Shields, 1f, 0f);
        }

        /// <summary>
        /// Adds only a station's firing weapons, without hull or shields: the defence of a station that is itself the
        /// attack target, whose durability the attack already rates as the target.
        /// </summary>
        public void AddStationWeapons(ForceComposition force, GameEntity station) =>
            force.Add(GetStationProfile(station), 0f, 0f, 1f, 0f);

        // An entity without hardpoints fires no weapons: it adds only its hull and shields.
        private UnitCombatProfile GetStationProfile(GameEntity station)
        {
            _stationWeapons.Clear();
            int weapons = 0;
            if (station.TryGetFacade(out IHardPointsFacade hardPoints))
                foreach (IHardPointStatus hardPoint in hardPoints.HardPoints)
                {
                    if (!hardPoint.IsInstalled || hardPoint.IsDestroyed ||
                        !hardPoint.TryGetWeaponType(out WeaponType weaponType)) continue;
                    _stationWeapons.TryGetValue(weaponType, out int count);
                    _stationWeapons[weaponType] = count + 1;
                    weapons++;
                }
            if (_stationProfiles.TryGetValue(station.Id, out (int Weapons, UnitCombatProfile Profile) cached) &&
                cached.Weapons == weapons) return cached.Profile;
            _stationLoadout.Clear();
            foreach (KeyValuePair<WeaponType, int> pair in _stationWeapons)
                _stationLoadout.Add(new WeaponLoadoutEntry(pair.Key, pair.Value));
            IHealthModelObserver health = station.HealthModel;
            UnitCombatProfile profile = _catalog.CreateStructure(health.ShipClass, health.Hull, health.Shields,
                _stationLoadout);
            _stationProfiles[station.Id] = (weapons, profile);
            return profile;
        }

        public void AddUnit(ForceComposition force, GameEntity entity)
        {
            IHealthModelObserver health = entity.HealthModel;
            UnitCombatProfile profile = _catalog.Get(entity.GetFacade<IUnitTypeFacade>().UnitTypeId);
            force.Add(profile, health.Hull, health.Shields, GetOffenseScale(health), GetShieldRegen(profile, health));
        }

        /// <summary>Adds a satellite or defend platform at its current health, rated by its platform type.</summary>
        public void AddStructure(ForceComposition force, GameEntity structure)
        {
            IHealthModelObserver health = structure.HealthModel;
            UnitCombatProfile profile = _catalog.GetPlatform(structure.GetFacade<IDefendPlatformTypeFacade>().Type);
            force.Add(profile, health.Hull, health.Shields, GetOffenseScale(health), GetShieldRegen(profile, health));
        }

        /// <summary>A unit that lost its shield generator regenerates nothing.</summary>
        public static float GetShieldRegen(UnitCombatProfile profile, IHealthModelObserver health) =>
            health.IsLostShieldGenerator ? 0f : profile.ShieldRegen;

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
