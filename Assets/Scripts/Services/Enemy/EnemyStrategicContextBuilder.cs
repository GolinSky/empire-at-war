using static EmpireAtWar.Utils.FormationConversion;
using EmpireAtWar.Models.Players;
using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Ship;
using UnityEngine;
using GameEntity = EmpireAtWar.Entities.BaseEntity.IEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Units;

namespace EmpireAtWar.Services.Enemy
{
    public sealed class EnemyStrategicContext
    {
        public EnemyStrategicContext(
            EnemyStrategicSnapshot snapshot,
            IReadOnlyList<IShipEntity> ships,
            Vector3 captureTarget,
            GameEntity enemyFleetTarget,
            GameEntity enemyBaseTarget,
            GameEntity ownBase,
            IReadOnlyDictionary<IShipEntity, GameEntity> receivers = null)
        {
            Snapshot = snapshot;
            Ships = ships;
            CaptureTarget = captureTarget;
            EnemyFleetTarget = enemyFleetTarget;
            EnemyBaseTarget = enemyBaseTarget;
            OwnBase = ownBase;
            Receivers = receivers;
        }

        public EnemyStrategicSnapshot Snapshot { get; }
        public IReadOnlyList<IShipEntity> Ships { get; }
        public Vector3 CaptureTarget { get; }
        public GameEntity EnemyFleetTarget { get; }
        public GameEntity EnemyBaseTarget { get; }
        public GameEntity OwnBase { get; }
        public IReadOnlyDictionary<IShipEntity, GameEntity> Receivers { get; }
    }

    public sealed class EnemyStrategicContextBuilder
    {
        private const float BASE_THREAT_RADIUS = 100f;

        private readonly IShipService _shipService;
        private readonly IReinforcementZonesSystem _reinforcementZonesSystem;
        private readonly ICaptureSitesSystem _captureSites;
        private readonly IEntityLocator _entityLocator;
        private readonly IGameModelObserver _gameModel;
        private readonly PlayerSlot _owner;
        private readonly IPlayerRoster _roster;

        public EnemyStrategicContextBuilder(
            IShipService shipService,
            IReinforcementZonesSystem reinforcementZonesSystem,
            ICaptureSitesSystem captureSites,
            IEntityLocator entityLocator,
            IGameModelObserver gameModel,
            PlayerSlot owner,
            IPlayerRoster roster)
        {
            _shipService = shipService;
            _reinforcementZonesSystem = reinforcementZonesSystem;
            _captureSites = captureSites;
            _entityLocator = entityLocator;
            _gameModel = gameModel;
            _owner = owner;
            _roster = roster;
        }

        public EnemyStrategicContext Build()
        {
            PlayerId self = _owner.Id;
            List<IShipEntity> ownShips = GetShips(ship => ship.Owner == self);
            FormationPoint fleetCenter = CalculateFleetCenter(ownShips);
            Vector3 origin = ToVector(fleetCenter);
            // Defending an owned site outranks new captures; raiding an operational site is the fallback.
            bool hasThreatenedSite = _captureSites.TryGetThreatenedSite(self, out Vector3 captureTarget);
            bool hasCaptureTarget = hasThreatenedSite ||
                TryGetClosestCaptureTarget(origin, out captureTarget) ||
                _captureSites.TryGetRaidTarget(self, origin, out captureTarget);
            GameEntity ownBase = FindClosestEntity(EntityRoles.IsPlayerBase, owner => owner == self, origin);
            Vector3 home = ownBase != null ? ownBase.GetFacade<IEntityTransformFacade>().Transform.position : origin;

            // With several enemies the AI commits to one of them so it does not split its fleet.
            PlayerId focusEnemy = FindFocusEnemy(home);
            GameEntity enemyBaseTarget = FindClosestEntity(EntityRoles.IsPlayerBase,
                owner => owner == focusEnemy,
                origin);
            // Any hostile ship is a fair fleet target; the closest one wins.
            GameEntity enemyFleetTarget = FindClosestEntity(EntityRoles.IsShip,
                owner => _roster.IsHostile(self, owner),
                origin);
            // Strength is compared against the whole team of the focused enemy.
            List<IShipEntity> focusTeamShips = GetShips(ship => _roster.IsAllied(focusEnemy, ship.Owner));
            List<IShipEntity> hostileShips = GetShips(ship => _roster.IsHostile(self, ship.Owner));
            int ownedCapturableZoneCount = _reinforcementZonesSystem.GetOwnedCapturableZoneCount(self);
            int enemyShipsNearOwnBase = CountShipsNearBase(
                hostileShips,
                ownBase,
                BASE_THREAT_RADIUS);

            EnemyStrategicSnapshot snapshot = new EnemyStrategicSnapshot(
                _gameModel.VictoryCondition,
                _owner.Difficulty,
                ownShips.Count,
                focusTeamShips.Count,
                hasCaptureTarget,
                enemyBaseTarget != null,
                ownBase != null,
                ownedCapturableZoneCount,
                enemyShipsNearOwnBase,
                hasThreatenedSite);
            Dictionary<IShipEntity, GameEntity> receivers =
                new Dictionary<IShipEntity, GameEntity>();
            foreach (IShipEntity ship in ownShips)
                receivers.Add(ship, _entityLocator.GetEntity(ship.EntityId));
            return new EnemyStrategicContext(
                snapshot,
                ownShips,
                captureTarget,
                enemyFleetTarget,
                enemyBaseTarget,
                ownBase,
                receivers);
        }

        /// <summary>
        /// The hostile player whose living station is closest to <paramref name="home"/>;
        /// once every hostile station is gone, the owner of the closest hostile ship.
        /// </summary>
        private PlayerId FindFocusEnemy(Vector3 home)
        {
            PlayerId self = _owner.Id;
            GameEntity closestStation = FindClosestEntity(EntityRoles.IsPlayerBase,
                owner => _roster.IsHostile(self, owner),
                home);
            if (closestStation != null)
            {
                return closestStation.Owner;
            }

            GameEntity closestShip = FindClosestEntity(EntityRoles.IsShip,
                owner => _roster.IsHostile(self, owner),
                home);
            return closestShip != null ? closestShip.Owner : PlayerId.None;
        }

        private bool TryGetClosestCaptureTarget(Vector3 origin, out Vector3 captureTarget)
        {
            bool hasZone = _reinforcementZonesSystem.TryGetCaptureTarget(
                _owner.Id, origin, out Vector3 zoneTarget);
            bool hasSite = _captureSites.TryGetCaptureTarget(
                _owner.Id, origin, out Vector3 siteTarget);
            captureTarget = hasSite && (!hasZone ||
                (siteTarget - origin).sqrMagnitude < (zoneTarget - origin).sqrMagnitude)
                ? siteTarget
                : zoneTarget;
            return hasZone || hasSite;
        }

        private List<IShipEntity> GetShips(Predicate<IShipEntity> include)
        {
            List<IShipEntity> ships = new List<IShipEntity>();
            foreach (IShipEntity ship in _shipService.Ships)
            {
                // A ship joins IShipService before its entity registers; the
                // registration raises EntityAdded, which re-evaluates the AI.
                if (include(ship) &&
                    _entityLocator.TryGetEntity(ship.EntityId, out GameEntity _))
                {
                    ships.Add(ship);
                }
            }

            return ships;
        }

        private static FormationPoint CalculateFleetCenter(IReadOnlyList<IShipEntity> ships)
        {
            if (ships.Count == 0)
            {
                return new FormationPoint(0f, 0f);
            }

            List<FormationPoint> positions = new List<FormationPoint>(ships.Count);
            foreach (IShipEntity ship in ships)
            {
                positions.Add(ToPoint(ship.WorldPosition));
            }

            return FormationModel.CalculateCenter(positions);
        }

        private static int CountShipsNearBase(
            IReadOnlyList<IShipEntity> ships,
            GameEntity ownBase,
            float threatRadius)
        {
            if (ownBase == null)
            {
                return 0;
            }

            Vector3 basePosition = ownBase.GetFacade<IEntityTransformFacade>().Transform.position;
            float threatRadiusSquared = threatRadius * threatRadius;
            int count = 0;
            foreach (IShipEntity ship in ships)
            {
                Vector3 offset = ship.WorldPosition - basePosition;
                offset.y = 0f;
                if (offset.sqrMagnitude <= threatRadiusSquared)
                {
                    count++;
                }
            }

            return count;
        }

        private GameEntity FindClosestEntity(Predicate<GameEntity> hasRole, Predicate<PlayerId> includeOwner, Vector3 origin)
        {
            GameEntity closest = null;
            float closestDistance = float.MaxValue;
            foreach (GameEntity entity in _entityLocator.Entities)
            {
                if (!includeOwner(entity.Owner) ||
                    !hasRole(entity) ||
                    entity.HealthModel.IsDestroyed ||
                    !entity.HealthModel.HasUnits)
                {
                    continue;
                }

                float distance = (entity.GetFacade<IEntityTransformFacade>().Transform.position - origin).sqrMagnitude;
                if (distance < closestDistance)
                {
                    closest = entity;
                    closestDistance = distance;
                }
            }

            return closest;
        }
    }
}
