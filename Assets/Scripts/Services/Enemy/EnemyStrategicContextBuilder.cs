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
using EmpireAtWar.Services.Ships;
using EmpireAtWar.Services.Vision;
using EmpireAtWar.Services.Stations;

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
        /// <summary>Closest known hostile near the fleet, ships before squadrons; null when none.</summary>
        public GameEntity LocalThreatTarget { get; }
        /// <summary>Closest known undefended hostile structure; null when none.</summary>
        public GameEntity RaidTarget { get; }
        public IReadOnlyDictionary<IShipEntity, GameEntity> Receivers { get; }

        public EnemyStrategicContext(
            IReadOnlyList<IShipEntity> ships,
            GameEntity enemyFleetTarget,
            GameEntity enemyBaseTarget,
            GameEntity ownBase,
            EnemyStrategicSnapshot snapshot,
            Vector3 captureTarget,
            IReadOnlyDictionary<IShipEntity, GameEntity> receivers = null,
            GameEntity localThreatTarget = null,
            GameEntity raidTarget = null)
        {
            Snapshot = snapshot;
            Ships = ships;
            CaptureTarget = captureTarget;
            EnemyFleetTarget = enemyFleetTarget;
            EnemyBaseTarget = enemyBaseTarget;
            OwnBase = ownBase;
            Receivers = receivers;
            LocalThreatTarget = localThreatTarget;
            RaidTarget = raidTarget;
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
        private readonly IVisionService _vision;
        // Structures never move, so one the team has seen stays a known raid target until it is destroyed.
        private readonly HashSet<long> _seenStructures = new HashSet<long>();
        // Targets stay picked while still valid: a new pick re-orders every committed ship, and each order makes
        // the ship's weapons re-sort their candidates.
        private GameEntity _localThreat;
        private GameEntity _raidTarget;

        private readonly PlayerSlot _owner;
        private readonly IProductionAccess _productionAccess;
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
            IVisionService vision,
            PlayerSlot owner,
            IProductionAccess productionAccess)
        {
            _vision = vision;
            _intel = intelRegistry.Get(owner.Team);
            _forceBuilder = forceBuilder;
            _shipService = shipService;
            _reinforcementZonesSystem = reinforcementZonesSystem;
            _captureSites = captureSites;
            _entityLocator = entityLocator;
            _gameModel = gameModel;
            _owner = owner;
            _productionAccess = productionAccess;
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
            PlayerId productionHost = _productionAccess.ProductionHost;
            GameEntity ownBase = FindClosestEntity(EntityRoles.IsPlayerBase, owner => owner == productionHost, origin);
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
            _localThreat = FindLocalThreat(origin, now);
            _raidTarget = FindRaidTarget(origin, now);
            GameEntity localThreatTarget = _localThreat;
            GameEntity raidTarget = _raidTarget;
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
                localAdvantage: CombatMatchup.Advantage(_ownForce, _localThreatForce),
                hasLocalThreat: localThreatTarget != null,
                hasRaidTarget: raidTarget != null);
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
                receivers: receivers,
                localThreatTarget: localThreatTarget,
                raidTarget: raidTarget);
        }

        /// <summary>
        /// The closest known hostile within <see cref="LOCAL_ENGAGEMENT_RADIUS"/>; ships win over squadrons.
        /// The previous pick stays while it is still known in range and no ship replaces a squadron pick.
        /// </summary>
        private GameEntity FindLocalThreat(Vector3 origin, float now)
        {
            GameEntity closest = null;
            bool closestIsShip = false;
            bool keepsPrevious = false;
            bool previousIsShip = false;
            float closestDistance = float.MaxValue;
            float radiusSquared = LOCAL_ENGAGEMENT_RADIUS * LOCAL_ENGAGEMENT_RADIUS;
            foreach (HostileSighting sighting in _intel.Sightings)
            {
                Vector3 offset = ToVector(sighting.Position) - origin;
                offset.y = 0f;
                float distance = offset.sqrMagnitude;
                bool isShip = sighting.UnitTypeId.IsShip;
                if (!_entityLocator.TryGetEntity(sighting.EntityId, out GameEntity entity) || entity.IsCloaked())
                {
                    continue;
                }

                if (_localThreat != null && sighting.EntityId == _localThreat.Id && distance <= radiusSquared &&
                    _intel.GetConfidence(sighting, now) > 0f)
                {
                    keepsPrevious = true;
                    previousIsShip = isShip;
                }

                if (distance > radiusSquared ||
                    closestIsShip && !isShip ||
                    isShip == closestIsShip && distance >= closestDistance ||
                    _intel.GetConfidence(sighting, now) <= 0f)
                {
                    continue;
                }

                closest = entity;
                closestIsShip = isShip;
                closestDistance = distance;
            }

            return keepsPrevious && (previousIsShip || !closestIsShip) ? _localThreat : closest;
        }

        /// <summary>
        /// The closest hostile structure (not a base) the team has seen, with no known hostile ship within
        /// <see cref="LOCAL_ENGAGEMENT_RADIUS"/> of it.
        /// </summary>
        private GameEntity FindRaidTarget(Vector3 origin, float now)
        {
            GameEntity closest = null;
            bool keepsPrevious = false;
            float closestDistance = float.MaxValue;
            foreach (GameEntity entity in _entityLocator.Entities)
            {
                if (!_playerRoster.IsHostile(_owner.Id, entity.Owner) ||
                    entity.IsUnit() ||
                    entity.IsPlayerBase() ||
                    entity.HealthModel.IsDestroyed ||
                    !entity.HealthModel.HasUnits)
                {
                    continue;
                }

                Vector3 position = entity.GetFacade<IEntityTransformFacade>().Transform.position;
                if (_vision.IsVisible(_owner.Id, position)) _seenStructures.Add(entity.Id);
                if (ReferenceEquals(entity, _raidTarget) && !IsDefended(position, now)) keepsPrevious = true;
                float distance = (position - origin).sqrMagnitude;
                if (distance >= closestDistance ||
                    !_seenStructures.Contains(entity.Id) ||
                    IsDefended(position, now))
                {
                    continue;
                }

                closest = entity;
                closestDistance = distance;
            }

            return keepsPrevious ? _raidTarget : closest;
        }

        private bool IsDefended(Vector3 position, float now)
        {
            float radiusSquared = LOCAL_ENGAGEMENT_RADIUS * LOCAL_ENGAGEMENT_RADIUS;
            foreach (HostileSighting sighting in _intel.Sightings)
            {
                Vector3 offset = ToVector(sighting.Position) - position;
                offset.y = 0f;
                if (sighting.UnitTypeId.IsShip &&
                    offset.sqrMagnitude <= radiusSquared &&
                    _intel.GetConfidence(sighting, now) > 0f)
                {
                    return true;
                }
            }

            return false;
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
                    !_entityLocator.TryGetEntity(sighting.EntityId, out GameEntity entity) ||
                    entity.IsCloaked())
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
