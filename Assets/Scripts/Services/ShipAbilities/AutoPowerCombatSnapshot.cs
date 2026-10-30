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
        private readonly List<(IEntity Entity, Vector3 Position, UnitCombatProfile Profile)> _units =
            new List<(IEntity, Vector3, UnitCombatProfile)>();
        private readonly Dictionary<(int X, int Z), List<int>> _cells = new Dictionary<(int, int), List<int>>();
        private readonly ForceComposition _own = new ForceComposition();
        private readonly ForceComposition _hostile = new ForceComposition();
        private readonly ForceComposition _engagedHostile = new ForceComposition();
        private readonly IEntityLocator _entities;
        private readonly ILocalPlayer _player;
        private readonly IVisionService _vision;
        private readonly UnitCombatProfileCatalog _profiles;

        public AutoPowerCombatSnapshot(IEntityLocator entities, ILocalPlayer player, IVisionService vision,
            UnitCombatProfileCatalog profiles)
        {
            _entities = entities; _player = player; _vision = vision; _profiles = profiles;
        }

        public void Capture()
        {
            _units.Clear();
            foreach (List<int> cell in _cells.Values) cell.Clear();
            foreach (IEntity entity in _entities.Entities)
                if (!entity.HealthModel.IsDestroyed && entity.TryGetFacade(out IUnitTypeFacade type))
                {
                    Vector3 position = entity.GetFacade<IEntityTransformFacade>().Transform.position;
                    var key = (Mathf.FloorToInt(position.x / CELL_SIZE), Mathf.FloorToInt(position.z / CELL_SIZE));
                    if (!_cells.TryGetValue(key, out List<int> cell))
                    {
                        cell = new List<int>();
                        _cells.Add(key, cell);
                    }
                    cell.Add(_units.Count);
                    _units.Add((entity, position, _profiles.Get(type.UnitTypeId)));
                }
        }

        public AutoPowerDecision Decide(IShipAbilityFacade caster, IShipAutoPowerFacade automation, bool heavyLoss,
            AutoPowerTuning tuning)
        {
            _own.Clear(); _hostile.Clear(); _engagedHostile.Clear();
            bool canAttack = false;
            bool hostileWarship = false;
            Vector3 origin = caster.WorldPosition;
            int minX = Mathf.FloorToInt((origin.x - caster.RadarRange) / CELL_SIZE);
            int maxX = Mathf.FloorToInt((origin.x + caster.RadarRange) / CELL_SIZE);
            int minZ = Mathf.FloorToInt((origin.z - caster.RadarRange) / CELL_SIZE);
            int maxZ = Mathf.FloorToInt((origin.z + caster.RadarRange) / CELL_SIZE);
            for (int x = minX; x <= maxX; x++)
            for (int z = minZ; z <= maxZ; z++)
            {
                if (!_cells.TryGetValue((x, z), out List<int> cell)) continue;
                foreach (int index in cell)
                {
                    var unit = _units[index];
                    IEntity entity = unit.Entity;
                    if (!_entities.TryGetEntity(entity.Id, out IEntity registered) || !ReferenceEquals(entity, registered) ||
                        entity.HealthModel.IsDestroyed || PlanarGeometry.Distance(caster.WorldPosition, unit.Position) > caster.RadarRange)
                        continue;
                    Vector3 position = entity.GetFacade<IEntityTransformFacade>().Transform.position;
                    if (PlanarGeometry.Distance(caster.WorldPosition, position) > caster.RadarRange) continue;
                    if (_player.IsHostile(entity.Owner))
                    {
                        if (entity.IsCloaked() || !_vision.IsVisible(_player.Id, position)) continue;
                        _hostile.Add(unit.Profile, entity.HealthModel.Hull, entity.HealthModel.Shields,
                            ForceCompositionBuilder.GetOffenseScale(entity.HealthModel));
                        if (automation.CanEngage(entity.HealthModel.ShipClass, position))
                        {
                            _engagedHostile.Add(unit.Profile, entity.HealthModel.Hull, entity.HealthModel.Shields,
                                ForceCompositionBuilder.GetOffenseScale(entity.HealthModel));
                            hostileWarship |= !entity.HealthModel.ShipClass.IsStrikecraft();
                            canAttack = true;
                        }
                    }
                    else if (_player.IsFriendly(entity.Owner))
                        _own.Add(unit.Profile, entity.HealthModel.Hull, entity.HealthModel.Shields,
                            ForceCompositionBuilder.GetOffenseScale(entity.HealthModel));
                }
            }
            ShipClass targetClass = caster.Health.ShipClass;
            UnitCombatProfile reference = _profiles.Get(UnitTypeId.Ship(ShipType.CorellianCorvette));
            float minimumDps = (reference.HullDps(targetClass) + reference.PiercingDps(targetClass)) * tuning.MinimumCorvetteForce;
            bool meaningful = (hostileWarship || _engagedHostile.UnitCount > 1) &&
                _engagedHostile.HullDps(targetClass) + _engagedHostile.PiercingDps(targetClass) >= minimumDps;
            return AutoPowerRules.Decide(caster.Health.HullPercentage, caster.Health.Shields, caster.Health.ShieldPercentage,
                automation.CanRegenerateShields, heavyLoss, meaningful, canAttack, CombatMatchup.Advantage(_hostile, _own),
                tuning.LowShields, tuning.LastStandHull, tuning.LastStandForceRatio);
        }
    }
}
