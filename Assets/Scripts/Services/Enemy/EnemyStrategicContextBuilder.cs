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
using EmpireAtWar.Services.Hazards;
using EmpireAtWar.Entities.Map.Hazards;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Presenters.ReinforcementZones;
using EmpireAtWar.Services.ShipNavigation;

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
        public EnemyCaptureObjective CaptureObjective { get; }
        public GameEntity DefenseTarget { get; }
        public string StationPressureReason { get; }
        public Vector3? RetreatAnchor { get; }
        public string RecoveryReason { get; }
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
            GameEntity raidTarget = null,
            EnemyCaptureObjective captureObjective = null,
            GameEntity defenseTarget = null,
            string stationPressureReason = "",
            Vector3? retreatAnchor = null,
            string recoveryReason = "")
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
            CaptureObjective = captureObjective;
            DefenseTarget = defenseTarget;
            StationPressureReason = stationPressureReason;
            RetreatAnchor = retreatAnchor;
            RecoveryReason = recoveryReason;
        }
    }

    public sealed class EnemyStrategicContextBuilder
    {
        private const float CAPTURE_STALL_SECONDS = 20f;

        /// <summary>Hostiles this close to the fleet center can engage it soon: twice the common ship weapon range (500).</summary>
        private const float LOCAL_ENGAGEMENT_RADIUS = 1000f;

        private readonly IShipService _shipService;
        private ISet<string> _reservedCaptureObjectives;
        private bool _groupScoped;
        private readonly IReinforcementZonesSystem _reinforcementZonesSystem;
        private readonly ICaptureSitesSystem _captureSites;
        private readonly IEntityLocator _entityLocator;
        private readonly IGameModelObserver _gameModel;
        private readonly IPlayerRoster _playerRoster;
        private readonly ForceCompositionBuilder _forceBuilder;
        private readonly HostileIntelModel _intel;
        private readonly IVisionService _vision;
        private readonly IHazardService _hazards;
        private readonly IReinforcementZoneSource _zones;
        private readonly CaptureSiteData _siteData;
        private readonly IShipNavigationService _navigation;
        private readonly ForceComposition _objectiveThreat = new ForceComposition();
        private readonly HashSet<long> _stationThreats = new HashSet<long>();
        private readonly List<EnemyCaptureObjective> _captureCandidates = new List<EnemyCaptureObjective>();
        private EnemyCaptureObjective _captureObjective;
        private float _captureDistance;
        private float _captureProgress;
        private float _captureStallSeconds;
        private float _captureEvaluatedAt;
        private bool _captureIsExecuting;
        private string _rejectedCaptureId;
        private float _captureRetryAt;
        private string _captureReason;
        private string _objectiveSafetyReason;
        private readonly List<IShipEntity> _ownShips = new List<IShipEntity>();
        private readonly List<FormationPoint> _formationPositions = new List<FormationPoint>();
        private readonly Dictionary<IShipEntity, GameEntity> _receivers = new Dictionary<IShipEntity, GameEntity>();
        private float _ionRiskWeight = 1f;
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
        private readonly ForceComposition _localOwnForce = new ForceComposition();

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
            IProductionAccess productionAccess,
            IHazardService hazards,
            IReinforcementZoneSource zones,
            CaptureSiteData siteData,
            IShipNavigationService navigation)
        {
            _vision = vision;
            _hazards = hazards;
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
            _zones = zones;
            _siteData = siteData;
            _navigation = navigation;
        }

        /// <summary>The returned collections are borrowed until the next Build on this player's builder.</summary>
        private EnemyStrategicContextBuilder(EnemyStrategicContextBuilder source)
        {
            _shipService = source._shipService;
            _reinforcementZonesSystem = source._reinforcementZonesSystem;
            _captureSites = source._captureSites;
            _entityLocator = source._entityLocator;
            _gameModel = source._gameModel;
            _playerRoster = source._playerRoster;
            _forceBuilder = source._forceBuilder;
            _intel = source._intel;
            _vision = source._vision;
            _hazards = source._hazards;
            _zones = source._zones;
            _siteData = source._siteData;
            _navigation = source._navigation;
            _owner = source._owner;
            _productionAccess = source._productionAccess;
        }

        public EnemyStrategicContextBuilder CreateGroupBuilder() => new EnemyStrategicContextBuilder(this);

        public EnemyStrategicContext Build(IReadOnlyList<IShipEntity> ships = null,
            ISet<string> reservedCaptureObjectives = null)
        {
            PlayerId self = _owner.Id;
            _groupScoped = ships != null;
            List<IShipEntity> ownShips;
            if (ships == null) ownShips = GetShips(self);
            else
            {
                _ownShips.Clear();
                _ownShips.AddRange(ships);
                ownShips = _ownShips;
            }
            _reservedCaptureObjectives = reservedCaptureObjectives;
            _ionRiskWeight = 1f;
            foreach (IShipEntity ship in ownShips)
            {
                IHealthModelObserver health = _entityLocator.GetEntity(ship.EntityId).HealthModel;
                float durability = health.Hull + health.Shields;
                if (durability <= 0f) continue;
                float capitalWeight = health.ShipClass == ShipClass.Capital || health.ShipClass == ShipClass.HeavyCapital ? 2f : 1f;
                _ionRiskWeight = Mathf.Max(_ionRiskWeight, 1f + capitalWeight * health.Shields / durability);
            }
            Vector3 origin = ToVector(CalculateFleetCenter(ownShips));
            PlayerId productionHost = _productionAccess.ProductionHost;
            GameEntity ownBase = FindClosestEntity(EntityRoles.IsPlayerBase, owner => owner == productionHost, origin);
            Vector3 home = ownBase != null ? ownBase.GetFacade<IEntityTransformFacade>().Transform.position : origin;
            PlayerId focusEnemy = FindFocusEnemy(home);
            GameEntity enemyBaseTarget = FindClosestEntity(EntityRoles.IsPlayerBase, owner => owner == focusEnemy, origin);
            float now = Time.time;
            GameEntity enemyFleetTarget = FindClosestKnownShip(origin, now);
            int focusTeamShipCount = CountKnownShips(focusEnemy, now);
            int ownedCapturableZoneCount = _reinforcementZonesSystem.GetOwnedCapturableZoneCount(self);
            _forceBuilder.Build(_ownForce, entity => entity.Owner == self &&
                (ships == null || ownShips.Exists(ship => ship.EntityId == entity.Id)));
            if (ships == null)
                _forceBuilder.Build(_ownTeamForce, entity => _playerRoster.IsAllied(self, entity.Owner));
            _forceBuilder.BuildKnown(_focusTeamForce, _intel, now,
                sighting => focusEnemy != PlayerId.None && _playerRoster.IsAllied(focusEnemy, sighting.Owner));
            GameEntity defenseTarget = BuildBaseThreat(ownBase);
            BuildHostilesNear(_localThreatForce, origin, LOCAL_ENGAGEMENT_RADIUS);
            BuildOwnNear(_localOwnForce, origin, LOCAL_ENGAGEMENT_RADIUS);
            _localThreat = FindLocalThreat(origin, now);
            _raidTarget = FindRaidTarget(origin, now);
            EnemyCaptureObjective captureObjective = SelectCaptureObjective(origin, now);
            Vector3? retreatAnchor = FindRetreatAnchor(origin, ownBase);
            string recoveryReason = retreatAnchor.HasValue
                ? "Validated a safe home or allied relay anchor."
                : "No safe home or allied relay; hold/rebuild or take a viable local objective.";
            float baseThreatRatio = _baseThreatForce.IsEmpty
                ? 0f
                : 1f / CombatMatchup.Advantage(_ownForce, _baseThreatForce);

            EnemyStrategicSnapshot snapshot = new EnemyStrategicSnapshot(
                victoryCondition: _gameModel.VictoryCondition,
                difficulty: _owner.Difficulty,
                ownShipCount: ownShips.Count,
                enemyShipCount: focusTeamShipCount,
                hasCaptureTarget: captureObjective != null,
                hasEnemyBaseTarget: enemyBaseTarget != null,
                hasOwnBase: ownBase != null,
                ownedCapturableZoneCount: ownedCapturableZoneCount,
                fleetAdvantage: CombatMatchup.Advantage(ships == null ? _ownTeamForce : _ownForce, _focusTeamForce),
                baseThreatRatio: baseThreatRatio,
                hasThreatenedSite: captureObjective != null && captureObjective.IsThreatened,
                localAdvantage: CombatMatchup.Advantage(_localOwnForce, _localThreatForce),
                hasLocalThreat: _localThreat != null,
                hasRaidTarget: _raidTarget != null,
                isRegrouped: IsRegrouped(ownShips),
                hasStationPressure: defenseTarget != null,
                hasSafeRetreat: retreatAnchor.HasValue);
            _receivers.Clear();
            foreach (IShipEntity ship in ownShips)
                _receivers.Add(ship, _entityLocator.GetEntity(ship.EntityId));
            return new EnemyStrategicContext(
                snapshot: snapshot,
                ships: ownShips,
                captureTarget: captureObjective != null ? captureObjective.Position : origin,
                enemyFleetTarget: enemyFleetTarget,
                enemyBaseTarget: enemyBaseTarget,
                ownBase: ownBase,
                receivers: _receivers,
                localThreatTarget: _localThreat,
                raidTarget: _raidTarget,
                captureObjective: captureObjective,
                defenseTarget: defenseTarget,
                stationPressureReason: defenseTarget == null ? "No visible hostile within station attack reach." :
                    defenseTarget.GetFacade<IUnitCombatObserverFacade>().IsAttacking(ownBase)
                        ? "Visible hostile is attacking the station within effective reach."
                        : "Visible hostile can reach the station footprint.",
                retreatAnchor: retreatAnchor,
                recoveryReason: recoveryReason + " " + _captureReason);
        }

        /// <summary>
        /// The closest visible hostile within <see cref="LOCAL_ENGAGEMENT_RADIUS"/>; ships win over squadrons.
        /// The previous pick stays while visible in range and no ship replaces a squadron pick.
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
                float score = GetTargetScore(origin, ToVector(sighting.Position));
                bool isShip = sighting.UnitTypeId.IsShip;
                if (!_entityLocator.TryGetEntity(sighting.EntityId, out GameEntity entity) || entity.IsCloaked() ||
                    !_vision.IsVisible(_owner.Id, entity.GetFacade<IEntityTransformFacade>().Transform.position))
                {
                    continue;
                }

                if (_localThreat != null && sighting.EntityId == _localThreat.Id && distance <= radiusSquared &&
                    _intel.GetConfidence(sighting, now) > 0f)
                {
                    keepsPrevious = GetIonCost(ToVector(sighting.Position)) == 0f;
                    previousIsShip = isShip;
                }

                if (distance > radiusSquared ||
                    closestIsShip && !isShip ||
                    isShip == closestIsShip && score >= closestDistance ||
                    _intel.GetConfidence(sighting, now) <= 0f)
                {
                    continue;
                }

                closest = entity;
                closestIsShip = isShip;
                closestDistance = score;
            }

            return keepsPrevious && (previousIsShip || !closestIsShip) ? _localThreat : closest;
        }

        /// <summary>
        /// The closest visible hostile structure (not a base), with no known hostile ship within
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
                if (!_vision.IsVisible(_owner.Id, position)) continue;
                if (ReferenceEquals(entity, _raidTarget) && !IsDefended(position, now) && GetIonCost(position) == 0f)
                    keepsPrevious = true;
                float distance = GetTargetScore(origin, position);
                if (distance >= closestDistance ||
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

        /// <summary>The visible hostile ship whose last known position is closest to <paramref name="origin"/>.</summary>
        private GameEntity FindClosestKnownShip(Vector3 origin, float now)
        {
            GameEntity closest = null;
            float closestDistance = float.MaxValue;
            foreach (HostileSighting sighting in _intel.Sightings)
            {
                if (!sighting.UnitTypeId.IsShip ||
                    _intel.GetConfidence(sighting, now) <= 0f ||
                    !_entityLocator.TryGetEntity(sighting.EntityId, out GameEntity entity) ||
                    entity.IsCloaked() ||
                    !_vision.IsVisible(_owner.Id, entity.GetFacade<IEntityTransformFacade>().Transform.position))
                {
                    continue;
                }

                float distance = GetTargetScore(origin, ToVector(sighting.Position));
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

        private float GetTargetScore(Vector3 origin, Vector3 position)
        {
            return (position - origin).sqrMagnitude * (1f + _ionRiskWeight * GetIonCost(position));
        }

        private float GetIonCost(Vector3 position)
        {
            float cost = 0f;
            IReadOnlyList<IHazardSource> hazards = _hazards.Sources;
            for (int i = 0; i < hazards.Count; i++)
            {
                IHazardSource hazard = hazards[i];
                if (hazard.Data.DisablesShields && hazard.Contains(position, 0f, false))
                    cost += hazard.Data.PathCost;
            }
            return cost;
        }

        private EnemyCaptureObjective SelectCaptureObjective(Vector3 origin, float now)
        {
            _captureCandidates.Clear();
            for (int i = 0; i < _zones.Zones.Count; i++)
            {
                ReinforcementZonePresenter zone = _zones.Zones[i];
                if (!zone.IsCapturable || _playerRoster.IsAllied(_owner.Id, zone.Owner)) continue;
                _captureCandidates.Add(new EnemyCaptureObjective($"zone:{i}", EnemyCaptureObjectiveType.ReinforcementZone,
                    zone.Owner, ToVector(ToPoint(zone.Center)), zone.Radius, zone.CaptureProgress));
            }
            for (int i = 0; i < _captureSites.Sites.Count; i++)
            {
                ICaptureSite site = _captureSites.Sites[i];
                bool threatened = site.Owner == _owner.Id && HasKnownHostileInside(site);
                bool capturable = site.State == CaptureSiteState.Neutral || site.State == CaptureSiteState.Owned;
                if (!threatened && (!capturable || _playerRoster.IsAllied(_owner.Id, site.Owner) ||
                    site.Kind == CaptureSiteKind.Satellite && !OwnsPointMines(site.PointId))) continue;
                _captureCandidates.Add(new EnemyCaptureObjective($"site:{i}", EnemyCaptureObjectiveType.Facility,
                    site.Owner, ToVector(ToPoint(site.Center)), _siteData.GetSettings(site.Kind).Radius,
                    site.CaptureProgress, threatened));
            }

            EnemyCaptureObjective best = null;
            EnemyCaptureObjective retained = null;
            float bestScore = float.MaxValue;
            _captureReason = "Ranking viable capture objectives.";
            foreach (EnemyCaptureObjective candidate in _captureCandidates)
            {
                if (_reservedCaptureObjectives != null && _reservedCaptureObjectives.Contains(candidate.Id)) continue;
                if (candidate.Id == _rejectedCaptureId && now < _captureRetryAt)
                {
                    AppendCaptureReason($"Rejected {candidate.Id}: stall cooldown {_captureRetryAt - now:F1}s remaining.");
                    continue;
                }
                if (!IsObjectiveSafe(origin, candidate.Position, candidate.Radius, false))
                {
                    AppendCaptureReason($"Rejected {candidate.Id}: {_objectiveSafetyReason}");
                    continue;
                }
                float score = GetTargetScore(origin, candidate.Position);
                if (best == null || candidate.IsThreatened && !best.IsThreatened ||
                    candidate.IsThreatened == best.IsThreatened && score < bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
                if (_captureObjective != null && candidate.Id == _captureObjective.Id &&
                    candidate.Owner == _captureObjective.Owner) retained = candidate;
            }

            if (retained != null && (best == null || !best.IsThreatened || retained.IsThreatened))
            {
                float distance = CaptureDistance(retained);
                if (_captureIsExecuting) _captureStallSeconds += now - _captureEvaluatedAt;
                _captureEvaluatedAt = now;
                if (distance + 1f < _captureDistance || Mathf.Abs(retained.Progress - _captureProgress) > 0.001f)
                {
                    _captureDistance = distance;
                    _captureProgress = retained.Progress;
                    _captureStallSeconds = 0f;
                }
                if (_captureStallSeconds < CAPTURE_STALL_SECONDS)
                {
                    _captureObjective = retained;
                    _captureReason += $" Retained {retained.Id}; approach/capture progress is monitored.";
                    return retained;
                }
                _rejectedCaptureId = retained.Id;
                _captureRetryAt = now + CAPTURE_STALL_SECONDS;
                _captureReason += $" Objective {retained.Id} stalled for {CAPTURE_STALL_SECONDS} seconds; considering alternatives.";
                best = null;
                bestScore = float.MaxValue;
                foreach (EnemyCaptureObjective candidate in _captureCandidates)
                {
                    if (_reservedCaptureObjectives != null && _reservedCaptureObjectives.Contains(candidate.Id)) continue;
                    if (candidate.Id == _rejectedCaptureId) continue;
                    if (!IsObjectiveSafe(origin, candidate.Position, candidate.Radius, false))
                    {
                        AppendCaptureReason($"Rejected {candidate.Id}: {_objectiveSafetyReason}");
                        continue;
                    }
                    float score = GetTargetScore(origin, candidate.Position);
                    if (best == null || candidate.IsThreatened && !best.IsThreatened ||
                        candidate.IsThreatened == best.IsThreatened && score < bestScore)
                    {
                        best = candidate;
                        bestScore = score;
                    }
                }
            }
            _captureObjective = best;
            _captureStallSeconds = 0f;
            _captureEvaluatedAt = now;
            _captureIsExecuting = false;
            _captureDistance = best == null ? 0f : CaptureDistance(best);
            _captureProgress = best == null ? 0f : best.Progress;
            _captureReason += best == null ? " No safe capture destination or approach is available." : $" Selected {best.Id}.";
            return best;
        }

        public void ReportCaptureExecution(EnemyCaptureObjective objective, float now)
        {
            _captureIsExecuting = objective != null && _captureObjective != null &&
                objective.Id == _captureObjective.Id && objective.Kind == _captureObjective.Kind &&
                objective.Owner == _captureObjective.Owner;
            _captureEvaluatedAt = now;
        }

        private void AppendCaptureReason(string reason)
        {
            if (_captureReason.Length < 512) _captureReason += " " + reason;
        }

        private float CaptureDistance(EnemyCaptureObjective objective)
        {
            float distance = float.MaxValue;
            foreach (IShipEntity ship in _ownShips)
                distance = Mathf.Min(distance, Mathf.Max(0f, PlanarDistance(ship.WorldPosition, objective.Position) - objective.Radius));
            return distance;
        }

        private bool HasKnownHostileInside(ICaptureSite site)
        {
            foreach (HostileSighting sighting in _intel.Sightings)
                if (_playerRoster.IsHostile(_owner.Id, sighting.Owner) &&
                    _intel.GetConfidence(sighting, Time.time) > 0f && site.Contains(ToVector(sighting.Position)))
                    return true;
            return false;
        }

        private bool OwnsPointMines(int pointId)
        {
            bool hasMine = false;
            foreach (ICaptureSite site in _captureSites.Sites)
            {
                if (site.Kind != CaptureSiteKind.Mining || site.PointId != pointId) continue;
                if (site.Owner != _owner.Id) return false;
                hasMine = true;
            }
            return hasMine;
        }

        private bool IsObjectiveSafe(Vector3 origin, Vector3 destination, float radius, bool escaping)
        {
            _objectiveSafetyReason = "";
            if (_ownShips.Count == 0)
            {
                _objectiveSafetyReason = "no ships are available.";
                return false;
            }
            float clearance = 0f;
            bool alreadyInside = false;
            foreach (IShipEntity ship in _ownShips)
            {
                clearance = Mathf.Max(clearance, ship.NavigationRadius);
                alreadyInside |= PlanarDistance(ship.WorldPosition, destination) <= radius;
            }
            // Retreat anchors are strategic areas; formation placement resolves transient ship reservations.
            if (!alreadyInside && !_navigation.IsPositionClear(destination, clearance, includeReservations: !escaping))
            {
                bool hasClearPoint = false;
                for (int i = 0; i < 4; i++)
                {
                    float angle = i * Mathf.PI * 0.5f;
                    Vector3 point = destination + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius * 0.5f;
                    hasClearPoint |= _navigation.IsPositionClear(point, clearance, includeReservations: !escaping);
                }
                if (!hasClearPoint)
                {
                    _objectiveSafetyReason = "no clear point in the objective area fits the fleet.";
                    return false;
                }
            }
            if (GetIonCost(destination) > GetIonCost(origin))
            {
                _objectiveSafetyReason = "destination ion cost exceeds the current position.";
                return false;
            }
            Vector3 segment = destination - origin;
            segment.y = 0f;
            _forceBuilder.BuildKnown(_objectiveThreat, _intel, Time.time, sighting =>
            {
                if (!_playerRoster.IsHostile(_owner.Id, sighting.Owner)) return false;
                Vector3 position = ToVector(sighting.Position);
                Vector3 offset = position - origin;
                offset.y = 0f;
                float t = segment.sqrMagnitude > 0f ? Mathf.Clamp01(Vector3.Dot(offset, segment) / segment.sqrMagnitude) : 0f;
                bool nearDestination = PlanarDistance(position, destination) <= LOCAL_ENGAGEMENT_RADIUS + radius;
                bool alongRoute = PlanarDistance(position, origin + segment * t) <= LOCAL_ENGAGEMENT_RADIUS;
                // An escape may leave the force at the origin, but cannot head into a second force ahead.
                return nearDestination || alongRoute && (!escaping || t > 0.25f);
            });
            if (_objectiveThreat.IsEmpty) return true;
            float advantage = CombatMatchup.Advantage(_groupScoped ? _ownForce : _localOwnForce, _objectiveThreat);
            float threshold = EnemyAiDifficultyProfile.Get(_owner.Difficulty).RetreatAdvantage;
            if (advantage > threshold) return true;
            _objectiveSafetyReason = $"known route/local force advantage {advantage:F2} <= safety threshold {threshold:F2}.";
            return false;
        }

        private Vector3? FindRetreatAnchor(Vector3 origin, GameEntity ownBase)
        {
            if (_ownShips.Count == 0) return null;
            // A surviving production host and its default home are distinct from a capturable relay.
            if (ownBase != null && _stationThreats.Count == 0 &&
                _reinforcementZonesSystem.TryGetDefaultZoneCenter(_productionAccess.ProductionHost, out Vector3 home) &&
                IsObjectiveSafe(origin, home, 0f, true)) return home;
            Vector3? closest = null;
            float closestScore = float.MaxValue;
            foreach (ReinforcementZonePresenter zone in _zones.Zones)
            {
                if (!zone.IsCapturable || !_playerRoster.IsAllied(_owner.Id, zone.Owner) || zone.IsContested) continue;
                Vector3 position = ToVector(ToPoint(zone.Center));
                float score = GetTargetScore(origin, position);
                if (score >= closestScore || !IsObjectiveSafe(origin, position, zone.Radius, true)) continue;
                closest = position;
                closestScore = score;
            }
            return closest;
        }

        private static float PlanarDistance(Vector3 a, Vector3 b)
        {
            Vector3 offset = a - b;
            offset.y = 0f;
            return offset.magnitude;
        }

        private List<IShipEntity> GetShips(PlayerId owner)
        {
            _ownShips.Clear();
            foreach (IShipEntity ship in _shipService.Ships)
            {
                // Ships join the service before their entity is registered.
                if (ship.Owner == owner && _entityLocator.TryGetEntity(ship.EntityId, out GameEntity _))
                    _ownShips.Add(ship);
            }
            return _ownShips;
        }

        private bool IsRegrouped(IReadOnlyList<IShipEntity> ships)
        {
            _formationPositions.Clear();
            for (int i = 0; i < ships.Count; i++) _formationPositions.Add(ToPoint(ships[i].WorldPosition));
            return FleetCohesion.IsTogether(_formationPositions, FleetCohesion.RADIUS);
        }

        private static FormationPoint CalculateFleetCenter(IReadOnlyList<IShipEntity> ships)
        {
            if (ships.Count == 0) return new FormationPoint(0f, 0f);
            float x = 0f;
            float z = 0f;
            for (int i = 0; i < ships.Count; i++)
            {
                FormationPoint position = ToPoint(ships[i].WorldPosition);
                x += position.X;
                z += position.Z;
            }
            return new FormationPoint(x / ships.Count, z / ships.Count);
        }

        /// <summary>Currently visible hostiles whose effective attack reach covers the station footprint.</summary>
        private GameEntity BuildBaseThreat(GameEntity ownBase)
        {
            _stationThreats.Clear();
            GameEntity target = null;
            float closest = float.MaxValue;
            if (ownBase != null)
            {
                Vector3 center = ownBase.GetFacade<IEntityTransformFacade>().Transform.position;
                float footprint = ownBase.GetFacade<INavigationFootprintFacade>().NavigationRadius;
                foreach (HostileSighting sighting in _intel.Sightings)
                {
                    if (!_playerRoster.IsHostile(_owner.Id, sighting.Owner) ||
                        _intel.GetConfidence(sighting, Time.time) <= 0f ||
                        !_entityLocator.TryGetEntity(sighting.EntityId, out GameEntity hostile) ||
                        hostile.HealthModel.IsDestroyed || !hostile.HealthModel.HasUnits || hostile.IsCloaked() ||
                        !hostile.TryGetFacade(out IUnitCombatObserverFacade combat)) continue;
                    Vector3 position = hostile.GetFacade<IEntityTransformFacade>().Transform.position;
                    if (!_vision.IsVisible(_owner.Id, position)) continue;
                    float hostileRadius = hostile.TryGetFacade(out INavigationFootprintFacade body) ? body.NavigationRadius : 0f;
                    float distance = PlanarDistance(position, center);
                    if (!CanThreatenStation(distance, footprint, hostileRadius, combat.WeaponRange)) continue;
                    // Hull-edge reach requires a commanded or weapon-tracked station attack. Other contacts must be
                    // close enough to engage from their centre or at their navigation contact distance.
                    if (!combat.IsAttacking(ownBase) && distance > Mathf.Max(combat.WeaponRange, footprint + hostileRadius)) continue;
                    _stationThreats.Add(hostile.Id);
                    bool attackingBase = combat.IsAttacking(ownBase);
                    bool previousAttacksBase = target != null && target.GetFacade<IUnitCombatObserverFacade>().IsAttacking(ownBase);
                    if (target == null || attackingBase && !previousAttacksBase ||
                        attackingBase == previousAttacksBase && distance < closest)
                    {
                        closest = distance;
                        target = hostile;
                    }
                }
            }
            _forceBuilder.BuildKnown(_baseThreatForce, _intel, Time.time, sighting => _stationThreats.Contains(sighting.EntityId));
            return target;
        }

        public static bool CanThreatenStation(float distance, float stationRadius, float hostileRadius, float weaponRange)
        {
            return weaponRange > 0f && distance <= stationRadius + hostileRadius + weaponRange;
        }

        /// <summary>Own ships and squadrons within <paramref name="radius"/> of <paramref name="center"/> (ground plane).</summary>
        private void BuildOwnNear(ForceComposition force, Vector3 center, float radius)
        {
            PlayerId self = _owner.Id;
            float radiusSquared = radius * radius;
            _forceBuilder.Build(force, entity =>
            {
                if (entity.Owner != self) return false;
                if (_groupScoped && !_ownShips.Exists(ship => ship.EntityId == entity.Id)) return false;
                Vector3 offset = entity.GetFacade<IEntityTransformFacade>().Transform.position - center;
                offset.y = 0f;
                return offset.sqrMagnitude <= radiusSquared;
            });
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
