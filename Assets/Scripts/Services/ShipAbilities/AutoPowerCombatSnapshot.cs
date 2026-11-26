using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.Ship.Abilities;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Extensions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Utils;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities
{
    public sealed class AutoPowerCombatSnapshot
    {
        private const float CELL_SIZE = 512f;
        private readonly List<IEntity> _units = new List<IEntity>();
        private readonly List<IEntity> _stations = new List<IEntity>();
        private readonly Dictionary<(int X, int Z), List<int>> _cells = new Dictionary<(int, int), List<int>>();
        private readonly List<(IEntity Entity, Vector3 Position)> _nearby = new List<(IEntity, Vector3)>();
        private readonly ForceComposition _own = new ForceComposition();
        private readonly ForceComposition _hostile = new ForceComposition();
        private readonly ForceComposition _engagedHostile = new ForceComposition();
        private readonly IEntityLocator _entities;
        private readonly IPlayerRelations _relations;
        private readonly ShipAbilityService _abilities;
        private readonly UnitCombatProfileCatalog _profiles;
        private readonly ForceCompositionBuilder _forces;
        private readonly AutoPowerTuning _tuning;
        private float _maxTargetFootprint;

        /// <summary>One snapshot serves every owner: each assessment reads the caster's own owner.</summary>
        public AutoPowerCombatSnapshot(IEntityLocator entities, IPlayerRelations relations, ShipAbilityService abilities,
            UnitCombatProfileCatalog profiles, ForceCompositionBuilder forces, ShipAbilityCatalog catalog)
        {
            _entities = entities;
            _relations = relations;
            _abilities = abilities;
            _profiles = profiles;
            _forces = forces;
            _tuning = catalog.AutoPower;
        }

        public void Capture()
        {
            _units.Clear();
            _stations.Clear();
            _maxTargetFootprint = 0f;
            foreach (List<int> cell in _cells.Values) cell.Clear();
            foreach (IEntity entity in _entities.Entities)
            {
                if (entity.HealthModel.IsDestroyed) continue;
                if (entity.IsPlayerBase())
                {
                    _maxTargetFootprint = Mathf.Max(_maxTargetFootprint, ShipAbilityRange.FootprintRadius(entity));
                    _stations.Add(entity);
                }
                else if (entity.TryGetFacade(out IUnitTypeFacade _))
                {
                    _maxTargetFootprint = Mathf.Max(_maxTargetFootprint, ShipAbilityRange.FootprintRadius(entity));
                    Vector3 position = entity.GetFacade<IEntityTransformFacade>().Transform.position;
                    var key = (Mathf.FloorToInt(position.x / CELL_SIZE), Mathf.FloorToInt(position.z / CELL_SIZE));
                    if (!_cells.TryGetValue(key, out List<int> cell))
                    {
                        cell = new List<int>();
                        _cells.Add(key, cell);
                    }
                    cell.Add(_units.Count);
                    _units.Add(entity);
                }
            }
        }

        /// <summary>Prefer the explicit attack target, then nearby hostiles in range.</summary>
        public IEntity FindTarget(IShipAbilityFacade caster, ShipAbilityDefinition definition)
        {
            if (definition.Settings is IShipAbilityTargetPriority priority)
                return FindPriorityTarget(caster, definition, priority);
            IUnitOrderObserverFacade order = caster.Entity.GetFacade<IUnitOrderObserverFacade>();
            if (order.CurrentOrder == UnitOrderType.Attack)
            {
                IEntity ordered = order.AttackTarget;
                if (ordered != null && IsLive(ordered) && _abilities.CanTarget(caster, definition, ordered) &&
                    ShipAbilityRange.IsInRange(caster, ordered, definition.Range))
                    return ordered;
                // Remote clients know the order type; authority resolves the target from the actual order.
                if (ordered != null && definition.IssuesAttackOrder)
                    return null;
            }
            IEntity target = null;
            bool targetStrikecraft = true;
            float nearest = float.PositiveInfinity;
            float searchRange = ShipAbilityRange.MaximumDistance(ShipAbilityRange.FootprintRadius(caster.Entity),
                _maxTargetFootprint, definition.Range);
            CollectNearby(caster.WorldPosition, searchRange);
            foreach ((IEntity entity, Vector3 position) in _nearby)
            {
                if (!_abilities.CanTarget(caster, definition, entity) ||
                    !ShipAbilityRange.IsInRange(caster, entity, definition.Range)) continue;
                bool strikecraft = entity.HealthModel.ShipClass.IsStrikecraft();
                float distance = PlanarGeometry.Distance(caster.WorldPosition, position);
                if (strikecraft && !targetStrikecraft || strikecraft == targetStrikecraft && distance >= nearest)
                    continue;
                target = entity;
                targetStrikecraft = strikecraft;
                nearest = distance;
            }
            return target;
        }

        // Highest priority in range, stations included; the attack-order target breaks ties, then the nearest.
        private IEntity FindPriorityTarget(IShipAbilityFacade caster, ShipAbilityDefinition definition,
            IShipAbilityTargetPriority priority)
        {
            IUnitOrderObserverFacade order = caster.Entity.GetFacade<IUnitOrderObserverFacade>();
            IEntity ordered = order.CurrentOrder == UnitOrderType.Attack ? order.AttackTarget : null;
            float searchRange = ShipAbilityRange.MaximumDistance(ShipAbilityRange.FootprintRadius(caster.Entity),
                _maxTargetFootprint, definition.Range);
            CollectNearby(caster.WorldPosition, searchRange);
            CollectNearbyStations(caster.WorldPosition, searchRange);
            IEntity target = null;
            int best = int.MinValue;
            float nearest = float.PositiveInfinity;
            foreach ((IEntity entity, Vector3 position) in _nearby)
            {
                if (!_abilities.CanTarget(caster, definition, entity) ||
                    !ShipAbilityRange.IsInRange(caster, entity, definition.Range)) continue;
                int rank = priority.GetTargetPriority(entity);
                float distance = ReferenceEquals(entity, ordered) ? -1f
                    : PlanarGeometry.Distance(caster.WorldPosition, position);
                if (rank < best || rank == best && distance >= nearest) continue;
                target = entity;
                best = rank;
                nearest = distance;
            }
            return target;
        }

        public AutoPowerRole Decide(IShipAbilityFacade caster, IShipAutoPowerFacade automation,
            AutoPowerShieldLoss shieldLoss) =>
            Assess(caster, automation, shieldLoss, travelling: false).Power;

        public AutoPowerSituation Assess(IShipAbilityFacade caster, IShipAutoPowerFacade automation,
            AutoPowerShieldLoss shieldLoss, bool travelling)
        {
            _own.Clear();
            _hostile.Clear();
            _engagedHostile.Clear();
            bool canAttack = false;
            bool hostileWarship = false;
            bool hostileShipNearby = false;
            PlayerId owner = caster.Entity.Owner;
            CollectNearby(caster.WorldPosition, caster.RadarRange);
            foreach ((IEntity entity, Vector3 position) in _nearby)
            {
                if (_abilities.IsVisibleHostile(owner, entity))
                {
                    _forces.AddUnit(_hostile, entity);
                    hostileShipNearby |= !entity.HealthModel.ShipClass.IsStrikecraft();
                    if (!automation.CanEngage(entity.HealthModel.ShipClass, position)) continue;
                    _forces.AddUnit(_engagedHostile, entity);
                    hostileWarship |= !entity.HealthModel.ShipClass.IsStrikecraft();
                    canAttack = true;
                }
                else if (_relations.IsAllied(owner, entity.Owner))
                    _forces.AddUnit(_own, entity);
            }
            ShipClass targetClass = caster.Health.ShipClass;
            UnitCombatProfile reference = _profiles.Get(UnitTypeId.Ship(ShipType.CorellianCorvette));
            float minimumDps = (reference.HullDps(targetClass) + reference.PiercingDps(targetClass)) * _tuning.MinimumCorvetteForce;
            bool meaningful = (hostileWarship || _engagedHostile.UnitCount > 1) &&
                _engagedHostile.HullDps(targetClass) + _engagedHostile.PiercingDps(targetClass) >= minimumDps;
            IHealthModelObserver health = caster.Health;
            bool hasShieldPower = false;
            bool shieldsBoosted = false;
            foreach (ShipAbilitySlot slot in caster.Slots)
            {
                if (slot.Definition.AutoPower != AutoPowerRole.Shields) continue;
                hasShieldPower = true;
                shieldsBoosted |= slot.State == ShipAbilityState.Active;
            }
            AutoPowerRole power = ChooseRole(health.HullPercentage, health.Shields, health.ShieldPercentage,
                hasShieldPower && automation.CanRegenerateShields, shieldsBoosted, shieldLoss, meaningful, canAttack,
                CombatMatchup.Advantage(_hostile, _own), hostileShipNearby);
            bool heavyLoss = shieldLoss == AutoPowerShieldLoss.Heavy;
            // A hostile station alone still counts as nearby, so a station-first ability can open on it.
            bool hostileNearby = !_hostile.IsEmpty || HasHostileStation(owner, caster.WorldPosition, caster.RadarRange);
            return new AutoPowerSituation(power, hostileNearby, engaged: meaningful && canAttack,
                endangered: heavyLoss && health.ShieldPercentage < _tuning.LowShields,
                cornered: heavyLoss && health.HullPercentage < _tuning.LastStandHull, travelling,
                urgent: IsUrgentShieldBoost(health.ShieldPercentage, hasShieldPower && automation.CanRegenerateShields,
                    shieldsBoosted, hostileShipNearby));
        }

        /// <summary>
        /// Shields below <see cref="AutoPowerTuning.ShieldBoostBelow"/> with a visible hostile warship (not
        /// strikecraft) in radar range: boost at once, before any other power choice.
        /// </summary>
        public bool IsUrgentShieldBoost(float shieldFraction, bool canBoostShields, bool shieldsBoosted,
            bool hostileShipNearby) =>
            canBoostShields && !shieldsBoosted && hostileShipNearby && shieldFraction < _tuning.ShieldBoostBelow;

        /// <summary>Priority: urgent Shields (low shields, warship near) → last-stand Weapons → Shields → normal Weapons.</summary>
        /// <remarks>
        /// Any shield damage below the low-shield line asks for Shields: a burst threshold alone can never be met
        /// once fewer shields remain than the burst, and steady fire never reaches it.
        /// Weapons may stack on an active shield boost; heavy loss then no longer holds it back.
        /// </remarks>
        public AutoPowerRole ChooseRole(float hull, float shields, float shieldFraction, bool canBoostShields,
            bool shieldsBoosted, AutoPowerShieldLoss shieldLoss, bool meaningfulForce, bool canAttack,
            float hostileAdvantage, bool hostileShipNearby = false)
        {
            if (IsUrgentShieldBoost(shieldFraction, canBoostShields, shieldsBoosted, hostileShipNearby))
                return AutoPowerRole.Shields;
            if (meaningfulForce && canAttack && hull < _tuning.LastStandHull && shields <= 0f &&
                hostileAdvantage >= _tuning.LastStandForceRatio)
                return AutoPowerRole.Weapons;
            if (canBoostShields && !shieldsBoosted && shieldLoss != AutoPowerShieldLoss.None &&
                shieldFraction < _tuning.LowShields)
                return AutoPowerRole.Shields;
            return meaningfulForce && canAttack && (shieldsBoosted || shieldLoss != AutoPowerShieldLoss.Heavy)
                ? AutoPowerRole.Weapons : AutoPowerRole.None;
        }

        // Cells come from the captured positions; range is checked against the live position.
        private void CollectNearby(Vector3 origin, float range)
        {
            _nearby.Clear();
            int minX = Mathf.FloorToInt((origin.x - range) / CELL_SIZE);
            int maxX = Mathf.FloorToInt((origin.x + range) / CELL_SIZE);
            int minZ = Mathf.FloorToInt((origin.z - range) / CELL_SIZE);
            int maxZ = Mathf.FloorToInt((origin.z + range) / CELL_SIZE);
            for (int x = minX; x <= maxX; x++)
            for (int z = minZ; z <= maxZ; z++)
            {
                if (!_cells.TryGetValue((x, z), out List<int> cell)) continue;
                foreach (int index in cell)
                {
                    IEntity entity = _units[index];
                    if (!IsLive(entity)) continue;
                    Vector3 position = entity.GetFacade<IEntityTransformFacade>().Transform.position;
                    if (PlanarGeometry.Distance(origin, position) <= range) _nearby.Add((entity, position));
                }
            }
        }

        // Stations are few and stay out of the unit grid: force composition rates units only.
        private void CollectNearbyStations(Vector3 origin, float range)
        {
            foreach (IEntity station in _stations)
            {
                if (!IsLive(station)) continue;
                Vector3 position = station.GetFacade<IEntityTransformFacade>().Transform.position;
                if (PlanarGeometry.Distance(origin, position) <= range) _nearby.Add((station, position));
            }
        }

        private bool HasHostileStation(PlayerId owner, Vector3 origin, float range)
        {
            foreach (IEntity station in _stations)
                if (IsLive(station) && _abilities.IsVisibleHostile(owner, station) &&
                    PlanarGeometry.Distance(origin, station.GetFacade<IEntityTransformFacade>().Transform.position) <= range)
                    return true;
            return false;
        }

        private bool IsLive(IEntity entity) =>
            _entities.TryGetEntity(entity.Id, out IEntity registered) && ReferenceEquals(entity, registered) &&
            !entity.HealthModel.IsDestroyed;
    }
}
