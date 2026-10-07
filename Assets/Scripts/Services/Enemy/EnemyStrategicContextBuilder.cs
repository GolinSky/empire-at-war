using static EmpireAtWar.Utils.FormationConversion;
using EmpireAtWar.Models.Players;
using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
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
        public EnemyStrategicSnapshot Snapshot { get; }
        public IReadOnlyList<IShipEntity> Ships { get; }
        public Vector3 CaptureTarget { get; }
        public GameEntity EnemyFleetTarget { get; }
        public GameEntity EnemyBaseTarget { get; }
        public GameEntity OwnBase { get; }
        public IReadOnlyDictionary<IShipEntity, GameEntity> Receivers { get; }

        public EnemyStrategicContext(
            IReadOnlyList<IShipEntity> ships,
            GameEntity enemyFleetTarget,
            GameEntity enemyBaseTarget,
            GameEntity ownBase,
            EnemyStrategicSnapshot snapshot,
            Vector3 captureTarget,
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
    }

    public sealed class EnemyStrategicContextBuilder
    {
        private const float BASE_THREAT_RADIUS = 100f;

        private readonly IShipService _shipService;
        private readonly IReinforcementZonesSystem _reinforcementZonesSystem;
        private readonly ICaptureSitesSystem _captureSites;
        private readonly IEntityLocator _entityLocator;
        private readonly IGameModelObserver _gameModel;
        private readonly IPlayerRoster _playerRoster;
        private readonly ForceCompositionBuilder _forceBuilder;

        private readonly PlayerSlot _owner;
        private readonly ForceComposition _ownForce = new ForceComposition();
        private readonly ForceComposition _focusTeamForce = new ForceComposition();
        private readonly ForceComposition _baseThreatForce = new ForceComposition();

        public EnemyStrategicContextBuilder(
            IShipService shipService,
            IReinforcementZonesSystem reinforcementZonesSystem,
            ICaptureSitesSystem captureSites,
            IEntityLocator entityLocator,
            IGameModelObserver gameModel,
            IPlayerRoster playerRoster,
            ForceCompositionBuilder forceBuilder,
            PlayerSlot owner)
        {
            _forceBuilder = forceBuilder;
            _shipService = shipService;
            _reinforcementZonesSystem = reinforcementZonesSystem;
            _captureSites = captureSites;
            _entityLocator = entityLocator;
            _gameModel = gameModel;
            _owner = owner;
            _playerRoster = playerRoster;
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
                owner => _playerRoster.IsHostile(self, owner),
                origin);
            // Strength is compared against the whole team of the focused enemy.
            List<IShipEntity> focusTeamShips = GetShips(ship => _playerRoster.IsAllied(focusEnemy, ship.Owner));
            int ownedCapturableZoneCount = _reinforcementZonesSystem.GetOwnedCapturableZoneCount(self);
            _forceBuilder.Build(_ownForce, entity => entity.Owner == self);
            _forceBuilder.Build(_focusTeamForce,
                entity => focusEnemy != PlayerId.None && _playerRoster.IsAllied(focusEnemy, entity.Owner));
            BuildBaseThreat(ownBase);
            float baseThreatRatio = _baseThreatForce.IsEmpty
                ? 0f
                : 1f / CombatMatchup.Advantage(_ownForce, _baseThreatForce);

            EnemyStrategicSnapshot snapshot = new EnemyStrategicSnapshot(
                victoryCondition: _gameModel.VictoryCondition,
                difficulty: _owner.Difficulty,
                ownShipCount: ownShips.Count,
                enemyShipCount: focusTeamShips.Count,
                hasCaptureTarget: hasCaptureTarget,
                hasEnemyBaseTarget: enemyBaseTarget != null,
                hasOwnBase: ownBase != null,
                ownedCapturableZoneCount: ownedCapturableZoneCount,
                fleetAdvantage: CombatMatchup.Advantage(_ownForce, _focusTeamForce),
                baseThreatRatio: baseThreatRatio,
                hasThreatenedSite: hasThreatenedSite);
            Dictionary<IShipEntity, GameEntity> receivers =
                new Dictionary<IShipEntity, GameEntity>();
            foreach (IShipEntity ship in ownShips)
                receivers.Add(ship, _entityLocator.GetEntity(ship.EntityId));
            return new EnemyStrategicContext(
                snapshot: snapshot,
                ships: ownShips,
                captureTarget: captureTarget,
                enemyFleetTarget: enemyFleetTarget,
                enemyBaseTarget: enemyBaseTarget,
                ownBase: ownBase,
                receivers: receivers);
        }

        /// <summary>
        /// The hostile player whose living station is closest to <paramref name="home"/>;
        /// once every hostile station is gone, the owner of the closest hostile ship.
        /// </summary>
        private PlayerId FindFocusEnemy(Vector3 home)
        {
            PlayerId self = _owner.Id;
            GameEntity closestStation = FindClosestEntity(EntityRoles.IsPlayerBase,
                owner => _playerRoster.IsHostile(self, owner),
                home);
            if (closestStation != null)
            {
                return closestStation.Owner;
            }

            GameEntity closestShip = FindClosestEntity(EntityRoles.IsShip,
                owner => _playerRoster.IsHostile(self, owner),
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

        /// <summary>Hostile ships and squadrons within <see cref="BASE_THREAT_RADIUS"/> of the own base.</summary>
        private void BuildBaseThreat(GameEntity ownBase)
        {
            if (ownBase == null)
            {
                _baseThreatForce.Clear();
                return;
            }

            PlayerId self = _owner.Id;
            Vector3 basePosition = ownBase.GetFacade<IEntityTransformFacade>().Transform.position;
            float threatRadiusSquared = BASE_THREAT_RADIUS * BASE_THREAT_RADIUS;
            _forceBuilder.Build(_baseThreatForce, entity =>
            {
                if (!_playerRoster.IsHostile(self, entity.Owner))
                {
                    return false;
                }

                Vector3 offset = entity.GetFacade<IEntityTransformFacade>().Transform.position - basePosition;
                offset.y = 0f;
                return offset.sqrMagnitude <= threatRadiusSquared;
            });
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
