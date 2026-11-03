using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Extensions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.Vision;
using EmpireAtWar.Utils;
using UnityEngine;

namespace EmpireAtWar.Services.ShipAbilities
{
    public sealed class AutoPowerCombatSnapshot
    {
        private const float CELL_SIZE = 512f;
        private readonly List<IEntity> _units = new List<IEntity>();
        private readonly Dictionary<(int X, int Z), List<int>> _cells = new Dictionary<(int, int), List<int>>();
        private readonly List<(IEntity Entity, Vector3 Position)> _nearby = new List<(IEntity, Vector3)>();
        private readonly ForceComposition _own = new ForceComposition();
        private readonly ForceComposition _hostile = new ForceComposition();
        private readonly ForceComposition _engagedHostile = new ForceComposition();
        private readonly IEntityLocator _entities;
        private readonly ILocalPlayer _player;
        private readonly IVisionService _vision;
        private readonly UnitCombatProfileCatalog _profiles;
        private readonly ForceCompositionBuilder _forces;
        private readonly AutoPowerTuning _tuning;

        public AutoPowerCombatSnapshot(IEntityLocator entities, ILocalPlayer player, IVisionService vision,
            UnitCombatProfileCatalog profiles, ForceCompositionBuilder forces, ShipAbilityCatalog catalog)
        {
            _entities = entities;
            _player = player;
            _vision = vision;
            _profiles = profiles;
            _forces = forces;
            _tuning = catalog.AutoPower;
        }

        public void Capture()
        {
            _units.Clear();
            foreach (List<int> cell in _cells.Values) cell.Clear();
            foreach (IEntity entity in _entities.Entities)
                if (!entity.HealthModel.IsDestroyed && entity.TryGetFacade(out IUnitTypeFacade _))
                {
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

        public IEntity FindBeamTarget(IShipAbilityFacade caster, float range)
        {
            IEntity target = null;
            float nearest = range;
            CollectNearby(caster.WorldPosition, range);
            foreach ((IEntity entity, Vector3 position) in _nearby)
            {
                float distance = PlanarGeometry.Distance(caster.WorldPosition, position);
                if (distance > nearest || !IsVisibleHostile(entity, position)) continue;
                target = entity;
                nearest = distance;
            }
            return target;
        }

        public AutoPowerRole Decide(IShipAbilityFacade caster, IShipAutoPowerFacade automation, bool heavyLoss)
        {
            _own.Clear();
            _hostile.Clear();
            _engagedHostile.Clear();
            bool canAttack = false;
            bool hostileWarship = false;
            CollectNearby(caster.WorldPosition, caster.RadarRange);
            foreach ((IEntity entity, Vector3 position) in _nearby)
            {
                if (IsVisibleHostile(entity, position))
                {
                    _forces.AddUnit(_hostile, entity);
                    if (!automation.CanEngage(entity.HealthModel.ShipClass, position)) continue;
                    _forces.AddUnit(_engagedHostile, entity);
                    hostileWarship |= !entity.HealthModel.ShipClass.IsStrikecraft();
                    canAttack = true;
                }
                else if (_player.IsFriendly(entity.Owner))
                    _forces.AddUnit(_own, entity);
            }
            ShipClass targetClass = caster.Health.ShipClass;
            UnitCombatProfile reference = _profiles.Get(UnitTypeId.Ship(ShipType.CorellianCorvette));
            float minimumDps = (reference.HullDps(targetClass) + reference.PiercingDps(targetClass)) * _tuning.MinimumCorvetteForce;
            bool meaningful = (hostileWarship || _engagedHostile.UnitCount > 1) &&
                _engagedHostile.HullDps(targetClass) + _engagedHostile.PiercingDps(targetClass) >= minimumDps;
            return ChooseRole(caster.Health.HullPercentage, caster.Health.Shields, caster.Health.ShieldPercentage,
                automation.CanRegenerateShields, heavyLoss, meaningful, canAttack, CombatMatchup.Advantage(_hostile, _own));
        }

        /// <summary>Priority: last-stand Weapons → Shields → normal Weapons.</summary>
        public AutoPowerRole ChooseRole(float hull, float shields, float shieldFraction, bool canRegenerate,
            bool heavyLoss, bool meaningfulForce, bool canAttack, float hostileAdvantage)
        {
            if (meaningfulForce && canAttack && hull < _tuning.LastStandHull && shields <= 0f &&
                hostileAdvantage >= _tuning.LastStandForceRatio)
                return AutoPowerRole.Weapons;
            if (canRegenerate && heavyLoss && shieldFraction < _tuning.LowShields)
                return AutoPowerRole.Shields;
            return meaningfulForce && canAttack && !heavyLoss ? AutoPowerRole.Weapons : AutoPowerRole.None;
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

        private bool IsLive(IEntity entity) =>
            _entities.TryGetEntity(entity.Id, out IEntity registered) && ReferenceEquals(entity, registered) &&
            !entity.HealthModel.IsDestroyed;

        private bool IsVisibleHostile(IEntity entity, Vector3 position) =>
            _player.IsHostile(entity.Owner) && !entity.IsCloaked() && _vision.IsVisible(_player.Id, position);
    }
}
