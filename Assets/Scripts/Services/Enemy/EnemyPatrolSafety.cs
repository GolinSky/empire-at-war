using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.EnemyFaction.Models.Intel;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.ShipNavigation;
using EmpireAtWar.Services.SpawnBlocking;
using UnityEngine;
using static EmpireAtWar.Utils.FormationConversion;

namespace EmpireAtWar.Services.Enemy
{
    /// <summary>A lone patrol avoids known weapon reach instead of borrowing the main fleet's combat strength.</summary>
    public sealed class EnemyPatrolSafety
    {
        private const float UNKNOWN_REACH = 500f;
        private const float REACH_MARGIN = 100f;
        private readonly HostileIntelModel _intel;
        private readonly EnemyPatrolMemory _memory;
        private readonly IEntityLocator _entities;
        private readonly IPlayerRoster _players;
        private readonly PlayerSlot _owner;
        private readonly IShipNavigationService _navigation;
        private readonly IMapModelObserver _map;
        private readonly IBattleAreaService _areas;
        private readonly ForceCompositionBuilder _forces;
        private readonly ForceComposition _ownForce = new ForceComposition();
        private readonly ForceComposition _threatForce = new ForceComposition();

        public EnemyPatrolSafety(TeamIntelRegistry registry, IEntityLocator entities, IPlayerRoster players,
            PlayerSlot owner, IShipNavigationService navigation, IMapModelObserver map, IBattleAreaService areas,
            ForceCompositionBuilder forces)
        {
            _intel = registry.Get(owner.Team);
            _memory = registry.GetPatrol(owner.Team);
            _entities = entities;
            _players = players;
            _owner = owner;
            _navigation = navigation;
            _map = map;
            _areas = areas;
            _forces = forces;
        }

        public bool IsObjectiveSafe(long unitId, Vector3 origin, Vector3 destination, float clearance, bool ship)
        {
            if (_areas.IsBlocked(_owner.Id, destination)) return false;
            if (ship && (!_navigation.IsPositionClear(destination, clearance, includeReservations: false) ||
                !_navigation.IsReachable(origin, destination, clearance, ShipHullSpan.Unbounded, _map.SizeRange))) return false;
            _forces.BuildKnown(_threatForce, _intel, Time.time, sighting =>
            {
                if (!_players.IsHostile(_owner.Id, sighting.Owner)) return false;
                float reach = UNKNOWN_REACH;
                if (_entities.TryGetEntity(sighting.EntityId, out IEntity entity) &&
                    entity.TryGetFacade(out IUnitCombatObserverFacade combat)) reach = combat.WeaponRange;
                return DistanceToSegment(ToVector(sighting.Position), origin, destination) < reach + REACH_MARGIN;
            });
            if (!_threatForce.IsEmpty)
            {
                _ownForce.Clear();
                _forces.AddUnit(_ownForce, _entities.GetEntity(unitId));
                if (CombatMatchup.Advantage(_ownForce, _threatForce) <=
                    EnemyAiDifficultyProfile.Get(_owner.Difficulty).RetreatAdvantage) return false;
            }
            foreach (EnemyPatrolPoint point in _memory.Entries)
            {
                if (!point.Id.StartsWith("structure:")) continue;
                long id = long.Parse(point.Id.Substring("structure:".Length));
                float reach = _entities.TryGetEntity(id, out IEntity entity) &&
                    entity.TryGetFacade(out IUnitCombatObserverFacade combat) ? combat.WeaponRange : UNKNOWN_REACH;
                if (DistanceToSegment(ToVector(point.Position), origin, destination) < reach + REACH_MARGIN) return false;
            }
            // Station locations are public map knowledge, including under fog.
            foreach (IEntity entity in _entities.Entities)
            {
                if (!entity.IsPlayerBase() || entity.HealthModel.IsDestroyed ||
                    !_players.IsHostile(_owner.Id, entity.Owner)) continue;
                float reach = entity.TryGetFacade(out IUnitCombatObserverFacade combat) ? combat.WeaponRange : UNKNOWN_REACH;
                if (DistanceToSegment(entity.GetFacade<IEntityTransformFacade>().Transform.position,
                        origin, destination) < reach + REACH_MARGIN) return false;
            }
            return true;
        }

        private static float DistanceToSegment(Vector3 point, Vector3 from, Vector3 to)
        {
            Vector3 segment = to - from;
            segment.y = 0f;
            Vector3 offset = point - from;
            offset.y = 0f;
            float t = segment.sqrMagnitude > 0f ? Mathf.Clamp01(Vector3.Dot(offset, segment) / segment.sqrMagnitude) : 0f;
            return (offset - segment * t).magnitude;
        }
    }
}
