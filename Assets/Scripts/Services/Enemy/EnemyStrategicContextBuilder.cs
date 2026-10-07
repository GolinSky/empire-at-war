using static EmpireAtWar.Utils.FormationConversion;
using EmpireAtWar.Models.Players;
using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.EnemyFaction.Models.Intel;
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

        /// <summary>Hostiles this close to the fleet center can engage it soon: twice the common ship weapon range (500).</summary>
        private const float LOCAL_ENGAGEMENT_RADIUS = 1000f;

        private readonly IShipService _shipService;
        private readonly IReinforcementZonesSystem _reinforcementZonesSystem;
        private readonly ICaptureSitesSystem _captureSites;
        private readonly IEntityLocator _entityLocator;
        private readonly IGameModelObserver _gameModel;
        private readonly IPlayerRoster _playerRoster;
        private readonly ForceCompositionBuilder _forceBuilder;
        private readonly HostileIntelModel _intel;

        private readonly PlayerSlot _owner;
        private readonly ForceComposition _ownForce = new ForceComposition();
        private readonly ForceComposition _ownTeamForce = new ForceComposition();
        private readonly ForceComposition _focusTeamForce = new ForceComposition();
        private readonly ForceComposition _baseThreatForce = new ForceComposition();
        private readonly ForceComposition _localThreatForce = new ForceComposition();

        public EnemyStrategicContextBuilder(
            IShipService shipService,
            IReinforcementZonesSystem reinforcementZonesSystem,
            ICaptureSitesSystem captureSites,
            IEntityLocator entityLocator,
            IGameModelObserver gameModel,
            IPlayerRoster playerRoster,
            ForceCompositionBuilder forceBuilder,
            TeamIntelRegistry intelRegistry,
            PlayerSlot owner)
        {
            _intel = intelRegistry.Get(owner.Team);
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
            // Hostiles are known only through the team's intel: what was seen, weighted by how fresh it is.
            float now = Time.time;
            // Any known hostile ship is a fair fleet target; the closest one still alive wins.
            GameEntity enemyFleetTarget = FindClosestKnownShip(origin, now);
            // Strength is compared against the whole team of the focused enemy.
            int focusTeamShipCount = CountKnownShips(focusEnemy, now);
            int ownedCapturableZoneCount = _reinforcementZonesSystem.GetOwnedCapturableZoneCount(self);
            _forceBuilder.Build(_ownForce, entity => entity.Owner == self);
            // Team games compare whole teams; alone against a 2-player team every AI looks outmatched and retreats.
            _forceBuilder.Build(_ownTeamForce, entity => _playerRoster.IsAllied(self, entity.Owner));
            _forceBuilder.BuildKnown(_focusTeamForce, _intel, now,
                sighting => focusEnemy != PlayerId.None && _playerRoster.IsAllied(focusEnemy, sighting.Owner));
            BuildBaseThreat(ownBase);
            // Retreat answers what is actually near the fleet, not the enemy's whole army across the map.
            BuildHostilesNear(_localThreatForce, origin, LOCAL_ENGAGEMENT_RADIUS);
            float baseThreatRatio = _baseThreatForce.IsEmpty
                ? 0f
                : 1f / CombatMatchup.Advantage(_ownForce, _baseThreatForce);

            EnemyStrategicSnapshot snapshot = new EnemyStrategicSnapshot(
                victoryCondition: _gameModel.VictoryCondition,
                difficulty: _owner.Difficulty,
                ownShipCount: ownShips.Count,
                enemyShipCount: focusTeamShipCount,
                hasCaptureTarget: hasCaptureTarget,
                hasEnemyBaseTarget: enemyBaseTarget != null,
                hasOwnBase: ownBase != null,
                ownedCapturableZoneCount: ownedCapturableZoneCount,
                fleetAdvantage: CombatMatchup.Advantage(_ownTeamForce, _focusTeamForce),
                baseThreatRatio: baseThreatRatio,
                hasThreatenedSite: hasThreatenedSite,
                localAdvantage: CombatMatchup.Advantage(_ownForce, _localThreatForce));
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

            GameEntity closestShip = FindClosestKnownShip(home, Time.time);
            return closestShip != null ? closestShip.Owner : PlayerId.None;
        }

        /// <summary>The living hostile ship whose last known position is closest to <paramref name="origin"/>.</summary>
        private GameEntity FindClosestKnownShip(Vector3 origin, float now)
        {
            GameEntity closest = null;
            float closestDistance = float.MaxValue;
            foreach (HostileSighting sighting in _intel.Sightings)
            {
                if (!sighting.UnitTypeId.IsShip ||
                    _intel.GetConfidence(sighting, now) <= 0f ||
                    !_entityLocator.TryGetEntity(sighting.EntityId, out GameEntity entity))
                {
                    continue;
                }

                float distance = (ToVector(sighting.Position) - origin).sqrMagnitude;
                if (distance < closestDistance)
                {
                    closest = entity;
                    closestDistance = distance;
                }
            }

            return closest;
        }

        private int CountKnownShips(PlayerId focusEnemy, float now)
        {
            int count = 0;
            foreach (HostileSighting sighting in _intel.Sightings)
            {
                if (sighting.UnitTypeId.IsShip &&
                    focusEnemy != PlayerId.None &&
                    _playerRoster.IsAllied(focusEnemy, sighting.Owner) &&
                    _intel.GetConfidence(sighting, now) > 0f)
                {
                    count++;
                }
            }

            return count;
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

            BuildHostilesNear(_baseThreatForce,
                ownBase.GetFacade<IEntityTransformFacade>().Transform.position, BASE_THREAT_RADIUS);
        }

        /// <summary>Hostile ships and squadrons within <paramref name="radius"/> of <paramref name="center"/> (ground plane).</summary>
        private void BuildHostilesNear(ForceComposition force, Vector3 center, float radius)
        {
            float radiusSquared = radius * radius;
            _forceBuilder.BuildKnown(force, _intel, Time.time, sighting =>
            {
                Vector3 offset = ToVector(sighting.Position) - center;
                offset.y = 0f;
                return offset.sqrMagnitude <= radiusSquared;
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
